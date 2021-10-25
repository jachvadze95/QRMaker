
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


            stlFile.Triangles.AddRange(BuildSquareBlock(0));

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

        static IEnumerable<StlTriangle> BuildSquareBlock(int index)
        {

            List<StlVertex> startingPoints = new List<StlVertex>(){
                new StlVertex(-1,-1,-1),
                new StlVertex(1,1,-1),
                new StlVertex(1,-1,1),
                new StlVertex(-1,1,1)
            };
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
            StlNormal normal;
            switch (plane)
            {
                case Plane.XY:
                    normal = new StlNormal(0, 0, point.Z);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X * -1, point.Y, point.Z), new StlVertex(point.X, point.Y * -1, point.Z));
                case Plane.YZ:
                    normal = new StlNormal(point.X, 0, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X, point.Y * -1, point.Z), new StlVertex(point.X, point.Y, point.Z * -1));
                case Plane.XZ:
                    normal = new StlNormal(0, point.Y, 0);
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
