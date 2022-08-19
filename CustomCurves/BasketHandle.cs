using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using static System.Math;

using AcRx = Autodesk.AutoCAD.Runtime;

namespace CustomCurves
{
    abstract class BasketHandle
    {
        protected double height, width;

        public BasketHandle(Point2d startPoint, Point2d endPoint, double height)
        {
            if (startPoint.GetDistanceTo(endPoint) <= 0.0)
                throw new AcRx.Exception(AcRx.ErrorStatus.DegenerateGeometry);
            if (height <= 0.0)
                throw new AcRx.Exception(AcRx.ErrorStatus.DegenerateGeometry);
            width = startPoint.GetDistanceTo(endPoint);
            this.height = height;
            Arcs = GetArcs();
            var vector = new Vector2d((startPoint.X + endPoint.X) / 2.0, (startPoint.Y + endPoint.Y) / 2.0);
            var angle = (startPoint - endPoint).Angle;
            var xform = Matrix2d.Displacement(vector) * Matrix2d.Rotation(angle, Point2d.Origin);
            foreach (var arc in Arcs)
            {
                arc.TransformBy(xform);
            }
        }

        public CircularArc2d[] Arcs { get; }

        public Polyline ToPolyline()
        {
            var polyline = new Polyline();
            int cnt = Arcs.Length;
            for (int i = 0; i < Arcs.Length; i++)
            {
                var arc = Arcs[i];
                polyline.AddVertexAt(i, arc.StartPoint, Tan((arc.EndAngle - arc.StartAngle) / 4.0), 0.0, 0.0);
            }
            polyline.AddVertexAt(cnt, Arcs[cnt - 1].EndPoint, 0.0, 0.0, 0.0);
            return polyline;
        }

        protected abstract CircularArc2d[] GetArcs();
    }

    class BasketHandleThreeCenters : BasketHandle
    {
        public BasketHandleThreeCenters(Point2d startPoint, Point2d endPoint, double height)
            : base(startPoint, endPoint, height) { }

        protected override CircularArc2d[] GetArcs()
        {
            double halfWidth = width / 2.0;
            var point1 = new Point2d(halfWidth, 0.0);
            var point3 = new Point2d(0.0, height);
            var segment = new LineSegment2d(point1, (point3 - point1).GetNormal() * (point1.GetDistanceTo(point3) - (halfWidth - height)));
            var bisector = segment.GetBisector();
            var center1 = bisector.IntersectWith(new Line2d(Point2d.Origin, Vector2d.XAxis))[0];
            var center2 = bisector.IntersectWith(new Line2d(Point2d.Origin, Vector2d.YAxis))[0];
            var center3 = new Point2d(-center1.X, center1.Y);
            var vector = height > halfWidth ? center2 - center1 : center1 - center2;
            var angle1 = Vector2d.XAxis.GetAngleTo(vector);
            var angle2 = (vector).GetAngleTo(Vector2d.YAxis) * 2.0;
            return new[]
            {
                new CircularArc2d(center1, center1.GetDistanceTo(point1), 0.0, angle1, Vector2d.XAxis, false),
                new CircularArc2d(center2, center2.GetDistanceTo(point3), angle1, angle1 + angle2, Vector2d.XAxis, false),
                new CircularArc2d(center3, center1.GetDistanceTo(point1), angle1 + angle2, PI, Vector2d.XAxis, false)
            };
        }
    }

    class BasketHandleSevenCenters : BasketHandle
    {
        public BasketHandleSevenCenters(Point2d startPoint, Point2d endPoint, double height)
            : base(startPoint, endPoint, height) { }

        protected override CircularArc2d[] GetArcs()
        {
            double halfWidth = width / 2.0;
            double angle = PI * 0.25;
            var point1 = new Point2d(halfWidth, 0.0);
            var point3 = new Point2d(halfWidth * Cos(angle), height * Sin(angle));
            var point5 = new Point2d(0.0, height);
            var point7 = new Point2d(-halfWidth * Cos(angle), height * Sin(angle));
            var point9 = new Point2d(-halfWidth, 0.0);
            var vector1 = (point3 - point1).GetNormal();
            var vector2 = (point5 - point1).GetNormal();
            var vector3 = (point5 - point3).GetNormal();
            var line1 = new Line2d(point1, Vector2d.YAxis + vector1);
            var line2 = new Line2d(point3, vector1 + vector2);
            var line3 = new Line2d(point3, vector2 + vector3);
            var line4 = new Line2d(point5, Vector2d.XAxis + vector3.Negate());
            var point2 = line1.IntersectWith(line2)[0];
            var point4 = line3.IntersectWith(line4)[0];
            var point6 = new Point2d(-point4.X, point4.Y);
            var point8 = new Point2d(-point2.X, point2.Y);
            double bulge1 = Tan(Vector2d.YAxis.GetAngleTo(point2 - point1) / 2.0);
            double bulge2 = Tan((point3 - point2).GetAngleTo(vector2) / 2.0);
            double bulge3 = Tan(vector2.GetAngleTo(point4 - point3) / 2.0);
            double bulge4 = Tan((point5 - point4).GetAngleTo(Vector2d.XAxis));
            return new[]
            {
                new CircularArc2d(point1, point2, bulge1, false),
                new CircularArc2d(point2, point3, bulge2, false),
                new CircularArc2d(point3, point4, bulge3, false),
                new CircularArc2d(point4, point6, bulge4, false),
                new CircularArc2d(point6, point7, bulge3, false),
                new CircularArc2d(point7, point8, bulge2, false),
                new CircularArc2d(point8, point9, bulge1, false)
            };
        }
    }
}
