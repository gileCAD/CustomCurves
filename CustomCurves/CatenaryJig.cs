using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using static System.Math;
using static CustomCurves.LanguageResource;

namespace CustomCurves
{
    class CatenaryJig : EntityJig
    {
        Spline spline;
        double tension, distance, minDist;
        Point3d basePt, startPt, endPt;
        Matrix3d ucs;
        int numPts;
        bool useTrig;

        public Double Tension => distance;

        public CatenaryJig(Spline spline, double tension, Matrix3d ucs, bool useTrig = false) : base(spline)
        {
            this.spline = spline;
            this.tension = tension;
            basePt = spline.StartPoint;
            this.ucs = ucs;
            var wcs = ucs.Inverse();
            startPt = spline.StartPoint.TransformBy(wcs);
            endPt = spline.EndPoint.TransformBy(wcs);
            numPts = spline.NumFitPoints;
            minDist = Abs((startPt.X - endPt.X) * 0.02);
            this.useTrig = useTrig;
        }

        public static IEnumerable<Point3d> FitPoints(Point3d startPoint, Point3d endPoint, int numFitPoints, double tension)
        {
            double length = endPoint.X - startPoint.X;
            double height = startPoint.Y - endPoint.Y;
            double step = length / (numFitPoints - 1);
            double alpha = height / (2.0 * tension * Sinh(length / (2.0 * tension)));
            double beta = (length / 2.0) + tension * Asinh(alpha);
            double gama = tension * (Cosh(beta / tension) - 1.0);
            double d1 = startPoint.X + beta;
            double d2 = startPoint.Y - gama;
            Point3d calcPoint(double x) =>
                new Point3d(x, tension * (Cosh((x - d1) / tension) - 1.0) + d2, 0.0);
            double xCoord = startPoint.X;
            yield return startPoint;
            for (int i = 0; i < numFitPoints - 2; i++)
            {
                yield return calcPoint(xCoord += step);
            }
            yield return endPoint;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptDistanceOptions($"\n{HorizontalTension} ");
            options.BasePoint = basePt;
            options.UseBasePoint = true;
            options.DefaultValue = tension;
            options.UserInputControls =
                UserInputControls.NoNegativeResponseAccepted |
                UserInputControls.NoZeroResponseAccepted |
                UserInputControls.NullResponseAccepted;
            options.Cursor = CursorType.RubberBand;
            var result = prompts.AcquireDistance(options);
            if (result.Value == distance || result.Value < minDist)
                return SamplerStatus.NoChange;
            distance = result.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            int i = 0;
            foreach (var pt in FitPoints(startPt, endPt, numPts, distance))
                spline.SetFitPointAt(i++, pt.TransformBy(ucs));
            return true;
        }

        static double Asinh(double a) => Log(a + Sqrt(a * a + 1.0));
    }
}
