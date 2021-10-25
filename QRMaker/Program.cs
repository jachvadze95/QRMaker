
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
        private static float _qrHeight = 0.5f;

        static void Main(string[] args)
        {
            StlFile stlFile = new StlFile();
            stlFile.SolidName = "my-solid";


            //stlFile.Triangles.AddRange(BuildSquareBlock(0));
            stlFile.Triangles.AddRange(BuildSquareBlock(1, 0));
            stlFile.Triangles.AddRange(BuildSquareBlock(2, 0));


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

        static IEnumerable<StlTriangle> BuildSquareBlock(int indexX, int indexY)
        {

            List<StlVertex> startingPoints = new List<StlVertex>(){
                new StlVertex(indexX, indexY, 0),
                new StlVertex(indexX + 1,indexY + 1, 0),
                new StlVertex(indexX + 1,indexY,_qrHeight),
                new StlVertex(indexX,indexY + 1,_qrHeight)
            };

            var response = new List<StlTriangle>();

            foreach (var nextPoint in startingPoints)
            {
                var xDirection = nextPoint.X > indexX ? -1 : 1;
                var yDirection = nextPoint.Y > indexY ? -1 : 1;

                foreach (var plane in Enum.GetValues(typeof(Plane)))
                {
                    response.Add(GetAdjacentTriangles(nextPoint, (Plane)plane, xDirection, yDirection));
                }
            }

            return response;
        }

        static StlTriangle GetAdjacentTriangles(StlVertex point, Plane plane, int xDirection, int yDirection)
        {
            StlNormal normal;
            bool isTop = point.Z > 0;
            var zDirection = isTop ? 0 : _qrHeight;

            switch (plane)
            {
                case Plane.XY:
                    normal = new StlNormal(0, 0, isTop ? 1 : -1);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X + xDirection, point.Y, point.Z), new StlVertex(point.X, point.Y + yDirection, point.Z));
                case Plane.YZ:
                    normal = new StlNormal(point.X > 0 ? 1 : -1, 0, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X, point.Y + yDirection, point.Z), new StlVertex(point.X, point.Y, zDirection));
                case Plane.XZ:
                    normal = new StlNormal(0, point.Y > 0 ? 1 : -1, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X + xDirection, point.Y, point.Z), new StlVertex(point.X, point.Y, zDirection));
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
