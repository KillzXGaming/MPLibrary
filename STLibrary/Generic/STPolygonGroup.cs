﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Toolbox.Library
{
    /// <summary>
    /// Stores a list of face indices and the capabily to map a material to them.
    /// This is used for when a mesh maps multiple materials to itself.
    /// </summary>
    public class STPolygonGroup
    {
        /// <summary>
        /// Gets or sets the index of the material.
        /// </summary>
        public int MaterialIndex { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="STGenericMaterial"/> 
        /// which determines how the mesh will be rendered.
        /// </summary>
        public STGenericMaterial Material { get; set; }

        /// <summary>
        /// Gets or sets a list of faces used to index vertices.
        /// </summary>
        public List<uint> Faces = new List<uint>();
    }
}