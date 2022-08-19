using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

using static System.Math;
using static CustomCurves.LanguageResource;

namespace CustomCurves
{
    class ParabolaJig : EntityJig
    {
        Point3d summit, dragPt, focus;
        Vector3d axis;
        Spline spline;

        public ParabolaJig(Spline spline, Vector3d axis) : base(spline)
        {
            this.spline = spline;
            this.axis = axis;
            summit = spline.GetControlPointAt(1);
        }

        public Point3d Focus => focus;

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options = new JigPromptPointOptions($"\n{BranchEnd} ");
            options.UserInputControls =
                UserInputControls.Accept3dCoordinates |
                UserInputControls.UseBasePointElevation;
            options.BasePoint = summit;
            var result = prompts.AcquirePoint(options);
            if (result.Value.IsEqualTo(dragPt))
                return SamplerStatus.NoChange;
            dragPt = result.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            var dist = summit.DistanceTo(dragPt) * Cos(axis.GetAngleTo(dragPt - summit));
            var ctrlPt = summit - axis * dist;
            spline.SetControlPointAt(0, dragPt);
            spline.SetControlPointAt(1, ctrlPt);
            spline.SetControlPointAt(2, dragPt.TransformBy(Matrix3d.Mirroring(summit + axis * dist)));

            double angle = axis.GetAngleTo(dragPt - ctrlPt);
            dist = ctrlPt.DistanceTo(dragPt) / 2.0 / Cos(angle);
            focus = ctrlPt + axis * dist;

            return true;
        }
    }
}
