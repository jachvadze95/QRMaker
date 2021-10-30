
using IxMilia.Stl;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace QRMaker
{
    class Program
    {
        private static float _qrHeight = 0.5f;
        private static float _cubeLength;
        private static string _qrBaseStlFileName = "base.stl";
        static void Main(string[] args)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode("https://kiai.ge/manual", QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);

            var baseStlPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $@"Files\{_qrBaseStlFileName}");
            var baseStl = OpenStl(baseStlPath);
            var normalizedBaseStl = NormalizedBaseStlTriangles(baseStl.Triangles);
            var baseMinMaxCoords = GetSolidMinMaxCoordinates(normalizedBaseStl);
            var baseWidth = Math.Max(baseMinMaxCoords.xMax, baseMinMaxCoords.xMin) - Math.Min(baseMinMaxCoords.xMax, baseMinMaxCoords.xMin);

            var qrCodeBitmap = qrCode.GetGraphic(1);

            var numberOfQrBlocksWidth = (float)qrCodeBitmap.Width;
            _cubeLength = baseWidth / numberOfQrBlocksWidth;

            StlFile stlFile = new StlFile();
            stlFile.SolidName = "my-solid";
            int borderDecrease = 3;

            for (int widthIndex = borderDecrease; widthIndex < qrCodeBitmap.Width - borderDecrease; widthIndex++)
            {
                for (int heightIndex = borderDecrease; heightIndex < qrCodeBitmap.Height - borderDecrease; heightIndex++)
                {
                    var currentPixel = qrCodeBitmap.GetPixel(heightIndex, widthIndex);
                    if (currentPixel.GetBrightness() != 0)
                    {
                        stlFile.Triangles.AddRange(BuildSquareBlock(widthIndex, heightIndex, zOffset: baseMinMaxCoords.zMax));
                    }
                }
            }
            stlFile.Triangles.AddRange(baseStl.Triangles);

            if (File.Exists("./file.stl"))
                File.Delete("./file.stl");

            using (FileStream fs = new FileStream("./file.stl", FileMode.OpenOrCreate))
            {
                stlFile.Save(fs);
            }
        }

        private static List<StlTriangle> NormalizedBaseStlTriangles(List<StlTriangle> triangles)
        {
            var normalizedTriangles = new List<StlTriangle>();

            var minMaxCoords = GetSolidMinMaxCoordinates(triangles);
            var xNormalizationConstant = minMaxCoords.xMax - Math.Abs(minMaxCoords.xMin) - Math.Abs(minMaxCoords.xMax);
            var yNormalizationConstant = minMaxCoords.yMax - Math.Abs(minMaxCoords.yMin) - Math.Abs(minMaxCoords.yMax);

            foreach (var triangle in triangles)
            {
                normalizedTriangles.Add(OffsetStlTriangle(triangle, xNormalizationConstant, yNormalizationConstant, 0f));
            }
            return normalizedTriangles;
        }

        static StlFile OpenStl(string filePath)
        {
            StlFile stlFile;
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                stlFile = StlFile.Load(fs);
                var groupedByNomal = stlFile.Triangles.GroupBy(x => x.Normal);
            }
            return stlFile;
        }

        static IEnumerable<StlTriangle> BuildSquareBlock(float indexX, float indexY, float baseXOffset = 0, float baseYOffset = 0, float zOffset = 0)
        {
            indexX *= _cubeLength;
            indexY *= _cubeLength;

            List<StlVertex> startingPoints = new List<StlVertex>(){
                new StlVertex(indexX, indexY, 0 ),
                new StlVertex(indexX + _cubeLength,indexY + _cubeLength, 0),
                new StlVertex(indexX + _cubeLength,indexY,_qrHeight),
                new StlVertex(indexX,indexY + _cubeLength,_qrHeight)
            };

            var response = new List<StlTriangle>();

            foreach (var nextPoint in startingPoints)
            {
                var xDirection = nextPoint.X > indexX ? -_cubeLength : _cubeLength;
                var yDirection = nextPoint.Y > indexY ? -_cubeLength : _cubeLength;

                foreach (var plane in Enum.GetValues(typeof(Plane)))
                {
                    response.Add(GetAdjacentTriangles(nextPoint, (Plane)plane, xDirection, yDirection));
                }
            }

            var shiftedQrTriangles = ShiftQrCoordinatesToBase(response, baseXOffset, baseYOffset, zOffset);
            return shiftedQrTriangles;
        }

        static StlTriangle GetAdjacentTriangles(StlVertex point, Plane plane, float xDirection, float yDirection)
        {
            StlNormal normal;
            bool isTop = point.Z > 0;
            var zDirection = isTop ? 0 : _qrHeight;

            switch (plane)
            {
                case Plane.XY:
                    normal = new StlNormal(0, 0, isTop ? _cubeLength : -_cubeLength);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X + xDirection, point.Y, point.Z), new StlVertex(point.X, point.Y + yDirection, point.Z));
                case Plane.YZ:
                    normal = new StlNormal(point.X > 0 ? _cubeLength : -_cubeLength, 0, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X, point.Y + yDirection, point.Z), new StlVertex(point.X, point.Y, zDirection));
                case Plane.XZ:
                    normal = new StlNormal(0, point.Y > 0 ? _cubeLength : -_cubeLength, 0);
                    return new StlTriangle(normal, new StlVertex(point.X, point.Y, point.Z), new StlVertex(point.X + xDirection, point.Y, point.Z), new StlVertex(point.X, point.Y, zDirection));
                default:
                    return null;
            }
        }

        public static MinMaxCoordinates GetSolidMinMaxCoordinates(List<StlTriangle> stlTriangles)
        {
            var allVertexCoordinates = new List<TriangleVertexCoordinates>();
            MinMaxCoordinates result = new MinMaxCoordinates();

            foreach (var triangle in stlTriangles)
            {
                allVertexCoordinates.AddRange(GetTriangleVertexCoordinates(triangle));
            }

            foreach (var vertexCoord in allVertexCoordinates)
            {
                var all = vertexCoord.Coords.Select(x => x);
                var coordsMin = vertexCoord.Coords.Min();
                var coordsMax = vertexCoord.Coords.Max();

                switch (vertexCoord.Axis)
                {

                    case Axis.X:
                        {
                            result.xMin = coordsMin < result.xMin ? coordsMin : result.xMin;
                            result.xMax = coordsMax > result.xMax ? coordsMax : result.xMax;
                        }
                        break;
                    case Axis.Y:
                        {
                            result.yMin = coordsMin < result.yMin ? coordsMin : result.yMin;
                            result.yMax = coordsMax > result.yMax ? coordsMax : result.yMax;
                        }
                        break;
                    case Axis.Z:
                        {
                            result.zMin = coordsMin < result.zMin ? coordsMin : result.zMin;
                            result.zMax = coordsMax > result.zMax ? coordsMax : result.zMax;
                        }
                        break;
                }
            }
            return result;
        }

        public static List<TriangleVertexCoordinates> GetTriangleVertexCoordinates(StlTriangle triangle)
        {
            var result = new List<TriangleVertexCoordinates>();
            var vertices = new List<StlVertex>();

            vertices.Add(triangle.Vertex1);
            vertices.Add(triangle.Vertex2);
            vertices.Add(triangle.Vertex3);

            foreach (var axis in Enum.GetValues(typeof(Axis)))
            {
                var coords = new List<float>();

                coords.AddRange(vertices.Select(x =>
                {
                    var coordField = x.GetType().GetField(axis.ToString());
                    return (float)coordField.GetValue(x);
                }));

                result.Add(new TriangleVertexCoordinates()
                {
                    Axis = (Axis)axis,
                    Coords = coords
                });
            }

            return result;
        }

        public static StlTriangle OffsetStlTriangle(StlTriangle stlTriangle, float xOffset, float yOffset, float zOffset)
        {
            stlTriangle.Vertex1 = new StlVertex(stlTriangle.Vertex1.X - xOffset, stlTriangle.Vertex1.Y - yOffset, stlTriangle.Vertex1.Z + zOffset);
            stlTriangle.Vertex2 = new StlVertex(stlTriangle.Vertex2.X - xOffset, stlTriangle.Vertex2.Y - yOffset, stlTriangle.Vertex2.Z + zOffset);
            stlTriangle.Vertex3 = new StlVertex(stlTriangle.Vertex3.X - xOffset, stlTriangle.Vertex3.Y - yOffset, stlTriangle.Vertex3.Z + zOffset);

            return stlTriangle;
        }

        public static List<StlTriangle> ShiftQrCoordinatesToBase(List<StlTriangle> stlTriangles, float xOffset, float yOffset, float zOffset)
        {
            var result = new List<StlTriangle>();
            foreach (var triangle in stlTriangles)
            {
                OffsetStlTriangle(triangle, xOffset, yOffset, zOffset);
                result.Add(triangle);
            }
            return result;
        }

        public enum Plane
        {
            XY = 0,
            YZ,
            XZ
        }

        public enum Axis
        {
            X,
            Y,
            Z
        }

        public class TriangleVertexCoordinates
        {
            public Axis Axis { get; set; }
            public List<float> Coords { get; set; }

        }

        public class MinMaxCoordinates
        {
            public float xMin { get; set; }
            public float xMax { get; set; }

            public float yMin { get; set; }
            public float yMax { get; set; }
            public float zMin { get; set; }
            public float zMax { get; set; }

        }
    }
}
