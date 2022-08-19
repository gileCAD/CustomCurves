using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using static System.Math;
using static CustomCurves.LanguageResource;


namespace CustomCurves
{
    class BasketHandleJig : EntityJig
    {
        Polyline pline;
        Point3d center;
        Point2d startPt, endPt;
        int numCenters;
        double height = 1.0;

        public BasketHandleJig(Polyline pline, int numCenters) : base(pline)
        {
            this.pline = pline;
            this.numCenters = numCenters;
            startPt = pline.GetPoint2dAt(0);
            endPt = pline.GetPoint2dAt(pline.NumberOfVertices - 1);
            center = new LineSegment3d(pline.StartPoint, pline.EndPoint).MidPoint;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptDistanceOptions($"\n{BasketHandleHeight} ");
            options.BasePoint = center;
            options.UseBasePoint = true;
            options.Cursor = CursorType.RubberBand;
            options.UserInputControls =
                UserInputControls.NoNegativeResponseAccepted |
                UserInputControls.NoZeroResponseAccepted;
            var result = prompts.AcquireDistance(options);
            if (result.Status == PromptStatus.Cancel)
                return SamplerStatus.Cancel;
            if (result.Value == height)
                return SamplerStatus.NoChange;
            height = result.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            BasketHandle basketHandle;
            switch (numCenters)
            {
                case 3:
                    basketHandle = new BasketHandleThreeCenters(startPt, endPt, height);
                    break;
                case 7:
                default:
                    basketHandle = new BasketHandleSevenCenters(startPt, endPt, height);
                    break;
            }
            for (int i = 0; i < basketHandle.Arcs.Length; i++)
            {
                var arc = basketHandle.Arcs[i];
                pline.SetPointAt(i, arc.StartPoint);
                pline.SetBulgeAt(i, Tan((arc.EndAngle - arc.StartAngle) / 4.0));
            }
            return true;
        }
    }
}
