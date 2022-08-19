using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using System.Linq;

using static CustomCurves.LanguageResource;
using static System.Math;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CustomCurves.CurveCommands))]

namespace CustomCurves
{

    public class CurveCommands
    {
        const string dictName = "GILE_CURVE";

        #region Commandes

        [CommandMethod("GILE_CURVES", "PARABOLA", "Parabola", CommandFlags.Modal)]
        public static void ParabolaCmd()
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            var ppo = new PromptPointOptions($"\n{Summit} ");
            var ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;
            var summit = ppr.Value;

            ppo.Message = $"\n{Axis} ";
            ppo.UseBasePoint = true;
            ppo.BasePoint = summit;
            ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
                return;
            var axis = (ppr.Value - summit).TransformBy(ed.CurrentUserCoordinateSystem).GetNormal();


            var pts = new Point3dCollection { summit, summit, summit };
            var knots = new DoubleCollection { 0.0, 0.0, 0.0, 1.0, 1.0, 1.0 };
            var weights = new DoubleCollection();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                using (var spline = new Spline(2, false, false, false, pts, knots, weights, 0.0, 0.0))
                {
                    spline.TransformBy(ed.CurrentUserCoordinateSystem);
                    var jig = new ParabolaJig(spline, axis);
                    var pr = ed.Drag(jig);
                    if (pr.Status == PromptStatus.OK)
                    {
                        var curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        curSpace.AppendEntity(spline);
                        tr.AddNewlyCreatedDBObject(spline, true);
                        var point = new DBPoint(jig.Focus);
                        curSpace.AppendEntity(point);
                        tr.AddNewlyCreatedDBObject(point, true);
                    }
                }
                tr.Commit();
            }
        }

        [CommandMethod("GILE_CURVES", "CATENARY", "Catenary", CommandFlags.Modal)]
        public static void CatenaryCmd()
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            PromptPointOptions pointOptions;
            PromptPointResult pointResult;
            var data = GetXrecordData("NumFitPoints");
            var numFitPoints = data == null ? 7 : (short)data.AsArray()[0].Value;
            while (true)
            {
                pointOptions = new PromptPointOptions($"\n{CurrentNumFitPoints} {numFitPoints}\n{StartPoint} ", "Points");
                pointOptions.Keywords.Default = "Points";
                pointResult = ed.GetPoint(pointOptions);
                if (pointResult.Status == PromptStatus.Keyword)
                {
                    var intOptions = new PromptIntegerOptions($"\n{NumFitPoints} ");
                    intOptions.DefaultValue = numFitPoints;
                    intOptions.UseDefaultValue = true;
                    intOptions.LowerLimit = 7;
                    intOptions.UpperLimit = 255;
                    var intResult = ed.GetInteger(intOptions);
                    if (intResult.Status != PromptStatus.OK)
                        return;
                    numFitPoints = intResult.Value;
                }
                else if (pointResult.Status != PromptStatus.OK)
                    return;
                else
                    break;
            }
            var pt1 = pointResult.Value;

            pointOptions.Message = $"\n{EndPoint} ";
            pointOptions.BasePoint = pt1;
            pointOptions.UseBasePoint = true;
            pointOptions.AppendKeywordsToMessage = false;
            pointOptions.Keywords.Clear();
            while (true)
            {
                pointResult = ed.GetPoint(pointOptions);
                if (pointResult.Status != PromptStatus.OK)
                    return;
                if (pointResult.Value.X == pt1.X)
                    ed.WriteMessage($"{PointError}");
                else
                    break;
            }
            var pt2 = pointResult.Value;
            data = GetXrecordData("Tension");
            var tension = data == null ? Abs(pt1.X - pt2.X) : (double)data.AsArray()[0].Value;

            Matrix3d ucs = ed.CurrentUserCoordinateSystem;
            Point3dCollection points = new Point3dCollection(
                CatenaryJig.FitPoints(pt1, pt2, numFitPoints, tension)
                .Select(p => p.TransformBy(ucs))
                .ToArray());
            using (Spline spline = new Spline(points, 3, 0.0))
            {
                var jig = new CatenaryJig(spline, tension, ucs);
                var dragResult = ed.Drag(jig);
                if (dragResult.Status == PromptStatus.OK)
                {
                    tension = jig.Tension;
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        space.AppendEntity(spline);
                        tr.AddNewlyCreatedDBObject(spline, true);
                        int i = 0;
                        foreach (var pt in CatenaryJig.FitPoints(pt1, pt2, numFitPoints, tension))
                        {
                            spline.SetFitPointAt(i++, pt.TransformBy(ucs));
                        }
                        SetXRecordData("NumFitPoints", new ResultBuffer(new TypedValue(70, numFitPoints)), tr);
                        SetXRecordData("Tension", new ResultBuffer(new TypedValue(40, tension)), tr);
                        tr.Commit();
                    }
                }
            }
        }

        [CommandMethod("GILE_CURVES", "BASKETHANDLE", "BasketHandle", CommandFlags.Modal)]
        public static void BasketHandleCmd()
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            var data = GetXrecordData("NumCenters");
            var numCenters = data == null ? 3 : (short)data.AsArray()[0].Value;
            var pointOptions = new PromptPointOptions($"\n{CurrentNumCenters} {numCenters}\n{BasketHandleStartPoint}");
            pointOptions.Keywords.Add(BasketHandleKeyword);
            pointOptions.Keywords.Default = BasketHandleKeyword;
            pointOptions.AppendKeywordsToMessage = true;
            PromptPointResult pointResult;
            while (true)
            {
                pointResult = ed.GetPoint(pointOptions);
                if (pointResult.Status == PromptStatus.Keyword)
                {
                    var kwOptions = new PromptKeywordOptions($"\n{NumCenters} ", "3 7");
                    kwOptions.Keywords.Default = numCenters.ToString();
                    var kwResult = ed.GetKeywords(kwOptions);
                    if (kwResult.Status != PromptStatus.OK)
                        return;
                    numCenters = int.Parse(kwResult.StringResult);
                }
                else if (pointResult.Status != PromptStatus.OK)
                    return;
                else
                    break;
            }
            var pt1 = pointResult.Value;

            pointOptions.Message = BasketHandleEndPoint;
            pointOptions.AppendKeywordsToMessage = false;
            pointOptions.BasePoint = pt1;
            pointOptions.UseBasePoint = true;
            pointResult = ed.GetPoint(pointOptions);
            if (pointResult.Status != PromptStatus.OK) return;
            var pt2 = pointResult.Value;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                SetXRecordData("NumCenters", new ResultBuffer(new TypedValue(70, numCenters)), tr);
                var ucs = ed.CurrentUserCoordinateSystem;
                var plane = new Plane();
                var startPt = pt2.Convert2d(plane);
                var endPt = pt1.Convert2d(plane);
                Polyline pline;
                switch (numCenters)
                {
                    case 3:
                        pline = new BasketHandleThreeCenters(startPt, endPt, 1.0).ToPolyline();
                        break;
                    case 7:
                    default:
                        pline = new BasketHandleSevenCenters(startPt, endPt, 1.0).ToPolyline();
                        break;
                }
                using (pline)
                {
                    pline.TransformBy(ucs);
                    var jig = new BasketHandleJig(pline, numCenters);
                    var pr = ed.Drag(jig);
                    if (pr.Status == PromptStatus.OK)
                    {
                        var curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        curSpace.AppendEntity(pline);
                        tr.AddNewlyCreatedDBObject(pline, true);
                    }
                }
                tr.Commit();
            }
        }

        [CommandMethod("GILE_CURVES", "RAMPANTARCH", "RampantArch", CommandFlags.Modal)]
        public static void RampantArchCmd()
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            var pointOptions = new PromptPointOptions($"\n{BasketHandleStartPoint}");
            var pointResult = ed.GetPoint(pointOptions);
            if (pointResult.Status != PromptStatus.OK)
                return;
            var basePoint = pointResult.Value;
            pointOptions.Message = $"\n{BasketHandleEndPoint}";
            pointOptions.BasePoint = basePoint;
            pointOptions.UseBasePoint = true;
            pointResult = ed.GetPoint(pointOptions);
            if (pointResult.Status != PromptStatus.OK)
                return;
            var endPoint = pointResult.Value;
            var ucs = ed.CurrentUserCoordinateSystem;
            var plane = new Plane(Point3d.Origin, ucs.CoordinateSystem3d.Zaxis);
            var referenceVector = ucs.CoordinateSystem3d.Xaxis.Convert2d(plane);
            using (var tr = db.TransactionManager.StartTransaction())
            using (var pline = new Polyline())
            {
                Point2d startPt, endPt;
                if (basePoint.X < endPoint.X)
                {
                    startPt = endPoint.Convert2d(new Plane());
                    endPt = basePoint.Convert2d(new Plane());
                }
                else
                {
                    startPt = basePoint.Convert2d(new Plane());
                    endPt = endPoint.Convert2d(new Plane());
                }
                var vector = (endPt - startPt) / 4.0;
                for (int i = 0; i < 5; i++)
                {
                    pline.AddVertexAt(i, startPt + i * vector, 0.0, 0.0, 0.0);
                }
                pline.TransformBy(ucs);
                var jig = new RampantArchJig(pline, referenceVector);
                var pr = ed.Drag(jig);
                if (pr.Status == PromptStatus.OK)
                {
                    var curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    curSpace.AppendEntity(pline);
                    tr.AddNewlyCreatedDBObject(pline, true);
                }
                tr.Commit();
            }
        }

        #endregion

        #region Méthodes privées

        static ResultBuffer GetXrecordData(string key)
        {
            var db = HostApplicationServices.WorkingDatabase;
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (!NOD.Contains(dictName))
                    return null;
                var dict = (DBDictionary)tr.GetObject(NOD.GetAt(dictName), OpenMode.ForRead);
                if (!dict.Contains(key))
                    return null;
                var xrec = (Xrecord)tr.GetObject(dict.GetAt(key), OpenMode.ForRead);
                return xrec.Data;
            }
        }

        static void SetXRecordData(string key, ResultBuffer data, Transaction tr)
        {
            var db = HostApplicationServices.WorkingDatabase;
            var NOD = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            DBDictionary dict;
            if (NOD.Contains(dictName))
            {
                dict = (DBDictionary)tr.GetObject(NOD.GetAt(dictName), OpenMode.ForRead);
            }
            else
            {
                dict = new DBDictionary();
                NOD.UpgradeOpen();
                NOD.SetAt(dictName, dict);
                tr.AddNewlyCreatedDBObject(dict, true);
            }
            Xrecord xrec;
            if (dict.Contains(key))
            {
                xrec = (Xrecord)tr.GetObject(dict.GetAt(key), OpenMode.ForWrite);
            }
            else
            {
                xrec = new Xrecord();
                dict.UpgradeOpen();
                dict.SetAt(key, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);
            }
            xrec.Data = data;
        }

        #endregion
    }
}
