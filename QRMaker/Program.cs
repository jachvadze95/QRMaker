
using IxMilia.Stl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;

namespace QRMaker
{
    class Program
    {
        static int _cubeHeight = 1;
        static int _cubeWidth = 1;
        static int _cubeLength = 1;

        static void Main(string[] args)
        {
            StlFile stlFile = new StlFile();
            stlFile.SolidName = "my-solid";

            

            for(int i=0; i < 1; i++)
            {
                stlFile.Triangles.AddRange(BuildSquareBlock(i));
            }


            using (FileStream fs = new FileStream(@"C:\Users\jachv\source\repos\QRMaker\QRMaker\bin\Debug\net5.0\ref\test.stl", FileMode.OpenOrCreate))
            {
                stlFile.Save(fs);
            }
        }

        static void OpenStl()
        {
            StlFile stlFile;
            using (FileStream fs = new FileStream(@"C:\Users\jachv\source\repos\QRMaker\QRMaker\bin\Debug\net5.0\ref\test1.stl", FileMode.Open))
            {
                stlFile = StlFile.Load(fs);

                var groupedByNomal = stlFile.Triangles.GroupBy(x => x.Normal);

            }
        }


        //static List<StlTriangle> Create3DRectangleOnPlane(Plane plane, Point start)
        //{
        //    var stlFile = new StlFile();

        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(), new StlVertex(start.X, start.Y, 0), new StlVertex(1, 0, 0), new StlVertex(1, 1, 0)));
        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(0, 0, -1), new StlVertex(0, 1, 0), new StlVertex(1, 1, 0), new StlVertex(0, 0, 0)));

        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(0, -1, 0), new StlVertex(1, 0, 0), new StlVertex(0, 0, 0), new StlVertex(0, 0, 1)));
        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(0, -1, 0), new StlVertex(1, 0, 0), new StlVertex(1, 0, 1), new StlVertex(0, 0, 1)));

        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(1, 0, 0), new StlVertex(0, 0, 0), new StlVertex(0, 1, 0), new StlVertex(0, 0, 1)));
        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(1, 0, 0), new StlVertex(0, 1, 0), new StlVertex(0, 1, 1), new StlVertex(0, 0, 1)));

        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(0, 0, 1), new StlVertex(0, 0, 1), new StlVertex(1, 0, 1), new StlVertex(1, 1, 1)));
        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(0, 0, 1), new StlVertex(0, 1, 1), new StlVertex(1, 1, 1), new StlVertex(0, 0, 1)));

        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(0, 1, 0), new StlVertex(1, 1, 0), new StlVertex(0, 1, 0), new StlVertex(0, 1, 1)));
        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(0, 1, 0), new StlVertex(1, 1, 0), new StlVertex(1, 1, 1), new StlVertex(0, 1, 1)));

        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(-1, 0, 0), new StlVertex(1, 0, 0), new StlVertex(1, 1, 0), new StlVertex(1, 0, 1)));
        //    stlFile.Triangles.Add(new StlTriangle(new StlNormal(-1, 0, 0), new StlVertex(1, 1, 0), new StlVertex(1, 1, 1), new StlVertex(1, 0, 1)));
        //}

        


        static IEnumerable<StlTriangle> BuildSquareBlock(int index)
        {

            List<StlVertex> startingPoints = new List<StlVertex>(){ 
                new StlVertex(0,0,0),
                new StlVertex(1,0,1),
                new StlVertex(0,1,1),
                new StlVertex(1,1,0)
            };

            
            startingPoints.ForEach(x => x.X += index);

            var response = new List<StlTriangle>();

            foreach (var nextPoint in startingPoints)
            {
                foreach (var plane in Enum.GetValues(typeof(Plane)))
                {
                    response.Add(GetAdjacentTriangles(nextPoint, (Plane)plane));
                }
            }

            return response;
        }
         
        static StlTriangle GetAdjacentTriangles(StlVertex point, Plane plane)
        {
            var normal = new StlNormal();

            switch (plane)
            {
                case Plane.XY:
                    normal = new StlNormal(0, 0, -1);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X * -1, point.Y, point.Z), new StlVertex(point.X, point.Y * -1, point.Z));
                case Plane.YZ:
                    normal = new StlNormal(-1, 0, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X, point.Y * -1, point.Z), new StlVertex(point.X, point.Y, point.Z * -1));
                case Plane.XZ:
                    normal = new StlNormal(0, -1, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X * -1, point.Y, point.Z), new StlVertex(point.X, point.Y, point.Z * -1));
                default:
                    return null;
            }
        }

        public enum Plane
        {
            XY = 0,
            YZ,
            XZ
        }
    }
}
