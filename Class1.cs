using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SurfaceComponents;
using System;
using System.Collections.Generic;

namespace Test
{

    public class Test : GH_Component
    {
        
        public Test() : base("Alpha Shape from Delaunay", "Test", "Creates a concave hull from a list of points", "Params", "Geometry")
        {
            
        }
        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh for normal and center point extraction", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCircleParameter("Centers", "C", "Circum-circles for all mesh triangles (quads are skipped)", GH_ParamAccess.list);
            //pManager.AddNumberParameter("Ratio", "R", "Ratio of triangles; altitude / longest edge. (quads are skipped)", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            if (!DA.GetData<Mesh>(0, ref mesh))
            {
                return;
            }
            var list = new System.Collections.Generic.List<object>();

            checked
            {

                var circles = new SurfaceComponents.MeshComponents.Component_MeshFaceCircles();
                var param = circles.Params.Input[0] as Grasshopper.Kernel.GH_PersistentGeometryParam<Grasshopper.Kernel.Types.GH_Mesh>;
                param.PersistentData.ClearData();
                param.PersistentData.Append(new GH_Mesh(mesh));

                circles.ExpireSolution(true);

                //add to a dummy document so we can read outputs
                var doc = new Grasshopper.Kernel.GH_Document();
                doc.AddObject(circles, false);

                //read output circles
                circles.Params.Output[0].CollectData();

                for(int i = 0; i < mesh.Faces.Count; ++i)
                {

                    list.Add(circles.Params.Output[0].VolatileData.get_Branch(0)[i]);

                }

                DA.SetDataList(0, list);

            }

        }

        public override Guid ComponentGuid
        {

            get { return new Guid("3D79563E-79F8-4BE6-A618-86018E9A9707"); }

        }

    }

}
