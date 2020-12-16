using Elements;
using ClipperLib;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnvelopeByCenterlines
{
    public static class EnvelopeByCenterlines
    {
        /// <summary>
        /// The EnvelopeByCenterlines function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A EnvelopeByCenterlinesOutputs instance containing computed results and the model with any new elements.</returns>
        public static EnvelopeByCenterlinesOutputs Execute(Dictionary<string, Model> inputModels, EnvelopeByCenterlinesInputs input)
        {
            var output = new EnvelopeByCenterlinesOutputs();
            var envMatl = new Material("envelope", new Color(0.3, 0.7, 0.7, 0.6), 0.0f, 0.0f);
            var validCenterlines = input.Centerlines.Where(c => c.Centerline != null);
            var distinctHeights = validCenterlines.Select(c => c.Height).Distinct().OrderBy(d => d);
            var currBase = 0.0;
            foreach (var height in distinctHeights)
            {
                var clinesBelowHeight = validCenterlines.Where(c => c.Height >= height);
                var individualPolygons = new List<Polygon>();
                foreach (var pg in clinesBelowHeight)
                {
                    var thickened = pg.Centerline.Offset(pg.Width / 2, EndType.Butt, 1E-08).First();
                    // if (pg.Centerline.Vertices.Count == 2)
                    // {
                    //     // butt ends are not perfectly perpendicular to the axis with clipper — for lines 

                    // }
                    if (thickened.IsClockWise())
                    {
                        thickened = thickened.Reversed();
                    }
                    individualPolygons.Add(thickened);
                }
                var union = individualPolygons.Count > 1 ? Polygon.UnionAll(individualPolygons) : individualPolygons;
                foreach (var polygon in union)
                {
                    var representation = new Representation(new SolidOperation[] { new Extrude(polygon, height - currBase, Vector3.ZAxis, false) });
                    var envelope = new Envelope(polygon, currBase, height - currBase, Vector3.ZAxis, 0, new Transform(0, 0, currBase), envMatl, representation, false, Guid.NewGuid(), "");
                    output.Model.AddElements(envelope);
                }
                output.Model.AddElements(clinesBelowHeight.Select(c => new Sketch(c.Centerline.TransformedPolyline(new Transform(0, 0, currBase)), Guid.NewGuid(), null)));
                currBase = height;
            }

            return output;

        }
    }
}