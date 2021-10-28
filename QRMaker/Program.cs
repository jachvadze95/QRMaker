
using IxMilia.Stl;
using QRCoder;
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
        private static float cubeLength = 1.2f;

        static void Main(string[] args)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode("https://kiai.ge/manual", QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);

            var qrRawData = qrCodeData.GetRawData(QRCodeData.Compression.GZip);
            var qrCodeBitmap = qrCode.GetGraphic(1);
 
            StlFile stlFile = new StlFile();
            stlFile.SolidName = "my-solid";
            int borderDecrease = 3;

            for (int widthIndex = borderDecrease; widthIndex < qrCodeBitmap.Width- borderDecrease; widthIndex++)
            {
                for (int heightIndex = borderDecrease; heightIndex < qrCodeBitmap.Height- borderDecrease; heightIndex++)
                {
                    var currentPixel = qrCodeBitmap.GetPixel(heightIndex, widthIndex);
                    if (currentPixel.GetBrightness() != 0)
                    {
                        stlFile.Triangles.AddRange(BuildSquareBlock(widthIndex,heightIndex));
                    }
                     
                }
            }

            using (FileStream fs = new FileStream("./file.stl", FileMode.OpenOrCreate))
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

        static IEnumerable<StlTriangle> BuildSquareBlock(float indexX, float indexY)
        {
            indexX *= cubeLength;
            indexY *= cubeLength;

            List<StlVertex> startingPoints = new List<StlVertex>(){
                new StlVertex(indexX, indexY, 0),
                new StlVertex(indexX + cubeLength,indexY + cubeLength, 0),
                new StlVertex(indexX + cubeLength,indexY,_qrHeight),
                new StlVertex(indexX,indexY + cubeLength,_qrHeight)
            };

            var response = new List<StlTriangle>();

            foreach (var nextPoint in startingPoints)
            {
                var xDirection = nextPoint.X > indexX ? -cubeLength : cubeLength;
                var yDirection = nextPoint.Y > indexY ? -cubeLength : cubeLength;

                foreach (var plane in Enum.GetValues(typeof(Plane)))
                {
                    response.Add(GetAdjacentTriangles(nextPoint, (Plane)plane, xDirection, yDirection));
                }
            }

            return response;
        }

        static StlTriangle GetAdjacentTriangles(StlVertex point, Plane plane, float xDirection, float yDirection)
        {
            StlNormal normal;
            bool isTop = point.Z > 0;
            var zDirection = isTop ? 0 : _qrHeight;

            switch (plane)
            {
                case Plane.XY:
                    normal = new StlNormal(0, 0, isTop ? cubeLength : -cubeLength);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X + xDirection, point.Y, point.Z), new StlVertex(point.X, point.Y + yDirection, point.Z));
                case Plane.YZ:
                    normal = new StlNormal(point.X > 0 ? cubeLength : -cubeLength, 0, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X, point.Y + yDirection, point.Z), new StlVertex(point.X, point.Y, zDirection));
                case Plane.XZ:
                    normal = new StlNormal(0, point.Y > 0 ? cubeLength : -cubeLength, 0);
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
