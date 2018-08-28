using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SurfaceComponents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcaveHull
{

    public class ConcaveHull : GH_Component
    {
        
        public ConcaveHull() : base("Concave Hull from Delaunay", "Concave Hull", "Creates a concave hull from a list of points", "Mesh", "Triangulation")
        {
            
        }
        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("PointsIn", "PtsIn", "Points to compute the concave hull", GH_ParamAccess.list);
            pManager.AddNumberParameter("Attrition", "Attr", "Manage the attrition of the concave hull", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("ConcaveHullOut", "CH", "Computed concave hull from input points", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            checked
            {

                var Points = new List<Point3d>();

                DA.GetDataList(0, Points);

                var attrition = 1.0;

                DA.GetData("Attrition", ref attrition);

                var nodeTwoList = new Grasshopper.Kernel.Geometry.Node2List(Points);
                var delaunayFaces = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Faces(nodeTwoList, 1);
                var delaunayMesh = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Mesh(nodeTwoList, 1, ref delaunayFaces);
                delaunayMesh.Weld(Math.PI);

                var list = new List<object>();

                var circles = new SurfaceComponents.MeshComponents.Component_MeshFaceCircles();
                var param = circles.Params.Input[0] as Grasshopper.Kernel.GH_PersistentGeometryParam<Grasshopper.Kernel.Types.GH_Mesh>;
                param.PersistentData.ClearData();
                param.PersistentData.Append(new GH_Mesh(delaunayMesh));

                circles.ExpireSolution(true);

                //add to a dummy document so we can read outputs
                var doc = new Grasshopper.Kernel.GH_Document();
                doc.AddObject(circles, false);

                //read output circles
                circles.Params.Output[0].CollectData();
                var ratio = new double[circles.Params.Output[1].VolatileDataCount];
                for (int i = 0; i < circles.Params.Output[0].VolatileDataCount; ++i)
                {

                    //ratio[i] = circles.Params.Output[1].VolatileData.get_Branch(0)[i];
                    GH_Convert.ToDouble(circles.Params.Output[1].VolatileData.get_Branch(0)[i], out ratio[i], GH_Conversion.Both);
                    list.Add(circles.Params.Output[0].VolatileData.get_Branch(0)[i]);

                }

                var arcsList = new double[list.Count];

                var arcs = new AnalysisComponents.Component_DeconstructArc();
                var arcParams = arcs.Params.Input[0] as Grasshopper.Kernel.GH_PersistentParam<Grasshopper.Kernel.Types.GH_Arc>;
                arcParams.PersistentData.ClearData();
                var circle = new Arc[delaunayMesh.Faces.Count];
                for (int i = 0; i < list.Count; ++i)
                {

                    GH_Convert.ToArc(list[i], ref circle[i], GH_Conversion.Both);
                    arcParams.PersistentData.Append(new GH_Arc(circle[i]));

                }

                arcs.ExpireSolution(true);
                var docOne = new Grasshopper.Kernel.GH_Document();
                docOne.AddObject(arcs, false);

                arcs.Params.Output[0].CollectData();
                for (int i = 0; i < arcs.Params.Output[1].VolatileDataCount; ++i)
                {

                    GH_Convert.ToDouble(arcs.Params.Output[1].VolatileData.get_Branch(0)[i], out arcsList[i], GH_Conversion.Both);
                    //arcsList.Add(arcs.Params.Output[1].VolatileData.get_Branch(0)[i]);

                }

                var faceMesh = new List<object>();
                var verticesMesh = new List<object>();

                var deMesh = new SurfaceComponents.MeshComponents.Component_DeconstructMesh();
                var meshParams = deMesh.Params.Input[0] as Grasshopper.Kernel.GH_PersistentParam<Grasshopper.Kernel.Types.GH_Mesh>;
                meshParams.PersistentData.ClearData();
                meshParams.PersistentData.Append(new GH_Mesh(delaunayMesh));

                deMesh.ExpireSolution(true);
                var docTwo = new Grasshopper.Kernel.GH_Document();
                docTwo.AddObject(deMesh, false);

                deMesh.Params.Output[0].CollectData();

                for (int i = 0; i < deMesh.Params.Output[0].VolatileDataCount; ++i)
                {

                    verticesMesh.Add(deMesh.Params.Output[0].VolatileData.get_Branch(0)[i]);

                }

                for (int i = 0; i < deMesh.Params.Output[1].VolatileDataCount; ++i)
                {

                    faceMesh.Add(deMesh.Params.Output[1].VolatileData.get_Branch(0)[i]);

                }

                var faceCullRadius = RadiusSorting(faceMesh, arcsList);

                Array.Sort(ratio);

                var splitListIndex = Convert.ToInt32((ratio[ratio.Length - 1] * faceMesh.Count) * attrition);
                //var splitListIndex = Convert.ToInt32(Attrition);

                var splitList = SplitList(faceCullRadius, splitListIndex);

                var constructMesh = new Mesh();
                var meshPoints = new Point3d[verticesMesh.Count];
                for (int i = 0; i < verticesMesh.Count; ++i)
                {

                    GH_Convert.ToPoint3d(verticesMesh[i], ref meshPoints[i], GH_Conversion.Both);
                    constructMesh.Vertices.Add(meshPoints[i]);

                }

                var meshFaces = new Grasshopper.Kernel.Types.GH_MeshFace[splitList.Count];
                for (int i = 0; i < splitList.Count; ++i)
                {

                    GH_Convert.ToGHMeshFace(splitList[i], GH_Conversion.Both, ref meshFaces[i]);
                    constructMesh.Faces.AddFace(meshFaces[i].Value);

                }

                var ConcaveHull = constructMesh.GetNakedEdges();

                DA.SetDataList(0, ConcaveHull);

            }

        }

        public override Guid ComponentGuid
        {

            get { return new Guid("3D79563E-79F8-4BE6-A618-86018E9A9707"); }

        }

        public List<object> RadiusSorting(List<object> x, double[] radius)
        {

            var compare = new List<KeyValuePair<double, object>>();

            for (int i = 0; i < x.Count; ++i)
            {

                compare.Add(new KeyValuePair<double, object>(radius[i], x.ElementAt(i)));

            }

            // Here is where the sorting according to radius takes place.
            compare.Sort(Compare1);
            compare.Reverse();
            // We retrieve a list of objects from our List of KeyValuePairs
            List<object> compared = (from kvp in compare select kvp.Value).Distinct().ToList();
            compared.Reverse();

            return compared;

        }

        static int Compare1(KeyValuePair<double, object> a, KeyValuePair<double, object> b)
        {

            return a.Key.CompareTo(b.Key);

        }

        private List<object> SplitList(List<object> list, int i)
        {

            var points = new List<object>();

            var chunk = Chunk(list, i, list.Count);

            foreach (var item in chunk)
            {

                points = item.ToList();

            }

            return points;

        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> source, int chunksize, int count)
        {

            while (source.Any())
            {

                yield return source.Take(chunksize);
                source = source.Skip(chunksize + count);

            }

        }

    }

}
