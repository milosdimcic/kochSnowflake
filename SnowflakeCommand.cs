using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace Snowflake
{
    public class SnowflakeCommand : Command
    {
        public SnowflakeCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SnowflakeCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "Snowflake"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //GET DATA FROM THE USER

                //Select multiple points
                ObjRef[] obj_refs;
                List<Point3d> finalPts = new List<Point3d>();

                var rc = RhinoGet.GetMultipleObjects("Select point", false, ObjectType.Point, out obj_refs);
                if(rc != Result.Success)
                    return rc;

                foreach(var o_ref in obj_refs)
                {
                    var point = o_ref.Point();
                    finalPts.Add(point.Location);
                }

                doc.Objects.UnselectAll();


            int depth = 0;
            var rc1 = RhinoGet.GetInteger("The Depth of the Snowflake?", false, ref depth, 0, 5);
            if(rc1 != Result.Success)
                return rc1;

            double radius = 10;
            var rc2 = RhinoGet.GetNumber("The size of the Snowflake?", false, ref radius);
            if(rc2 != Result.Success)
                return rc2;

            //CREATE SNOWFLAKES FOR EACH SELECTED POINT AND DEPTH AND RADIUS
            foreach(Point3d pt in finalPts)
            {
                //STEP 1 - Create the initial triangle (just a list of 3 points)
                    List<Point3d> triangle = GenerateSnowflakeTriangle(pt, radius);

                //STEP 2 - FROM TRIANGLE TO SNOWFLAKE
                    List<Point3d> snowflake = GenerateSnowflake(triangle, depth, doc);
            }


            return Result.Success;
        }

        private List<Point3d> GenerateSnowflake(List<Point3d> triangle, int depth, RhinoDoc doc)
        {
            List<Point3d> snowflake = new List<Point3d>();

            for(int i=1; i<triangle.Count +1; i++)
            {
                Point3d pt1 = triangle[ (i-1)%3 ];
                Point3d pt2 = triangle[ i% 3];

                double dx = pt2.X - pt1.X;
                double dy = pt2.Y - pt1.Y;

                double length = Math.Sqrt(dx*dx + dy*dy);
                double theta = Math.Atan2(dy, dx);

                //RECURSIVE FUNCTION
                DrawSnowflakeEdge(depth, ref pt1, theta, length, doc);
            }

            return snowflake;
            
        }

        private void DrawSnowflakeEdge(int depth, ref Point3d pt1, double theta, double length, RhinoDoc doc)
        {
            //WHEN TO STOP? When depth is 0
            if(depth ==0 )
            {
                Point3d pt2 = new Point3d(
                    (pt1.X + length * Math.Cos(theta)),
                    (pt1.Y + length * Math.Sin(theta)),
                    0);

                doc.Objects.AddLine(new Line(pt1, pt2));
                pt1 = pt2;
                return;
            }

            //one edge gets divided in thirds
            length *= (double)1/3; //0.333333333

            //list of angles for each side - 0,-60,120,-60 degrees
            List<double> generatorTheta = new List<double>() { 0, -Math.PI/3, 2 * Math.PI / 3, -Math.PI /3};

            for(int i=0; i< generatorTheta.Count; i++)
            {
                theta += generatorTheta[i];
                DrawSnowflakeEdge( depth - 1 , ref pt1, theta, length, doc);
            }
        }

        private List<Point3d> GenerateSnowflakeTriangle(Point3d pt, double radius)
        {
            Point3d pt1 = pt - (Vector3d.YAxis * radius) / 2;
            Point3d pt2 = pt1;
                    pt2.Transform(Transform.Rotation(2*Math.PI/3, pt));
            Point3d pt3 = pt1;
                    pt3.Transform(Transform.Rotation(-2*Math.PI / 3, pt));

            List<Point3d> triangle = new List<Point3d>() { pt1, pt2, pt3};

            return triangle;

        }
    }
}
