using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Geometry;
using static System.Math;

namespace CustomCurves
{
    class RampantArch
    {
        public CircularArc2d[] Arcs { get; }

        public RampantArch(Point2d startPoint, Point2d endPoint, double height, Vector2d referenceVector)
        {
            var line1 = new Line2d(startPoint, referenceVector);
            var line5 = new Line2d(endPoint, referenceVector);
            var segment = new LineSegment2d(endPoint, startPoint);
            double radius = segment.Length / 2.0;
            double delta = radius - height;
            var summit = segment.MidPoint + referenceVector.GetPerpendicularVector() * height;
            var line3 = segment.GetPerpendicularLine(summit);
            segment = new LineSegment2d(startPoint, summit);
            var line2 = new LineSegment2d(summit - segment.Direction * delta, startPoint).GetBisector();
            segment = new LineSegment2d(summit, endPoint);
            var line4 = new LineSegment2d(endPoint, summit + segment.Direction * delta).GetBisector();
            var center1 = line1.IntersectWith(line2)[0];
            var center2 = line2.IntersectWith(line3)[0];
            var center3 = line3.IntersectWith(line4)[0];
            var center4 = line4.IntersectWith(line5)[0];
            var angle1 = referenceVector.GetAngleTo(line2.Direction);
            var angle2 = referenceVector.GetAngleTo(line3.Direction);
            var angle3 = referenceVector.GetAngleTo(line4.Direction);
            Arcs = new CircularArc2d[4]
            {
                new CircularArc2d(center1, center1.GetDistanceTo(startPoint), 0.0, angle1, referenceVector, false),
                new CircularArc2d(center2, center2.GetDistanceTo(summit), angle1, angle2, referenceVector, false),
                new CircularArc2d(center3, center3.GetDistanceTo(summit), angle2, angle3, referenceVector, false),
                new CircularArc2d(center4, center4.GetDistanceTo(endPoint), angle3, PI, referenceVector, false)
            };
        }
    }
}
