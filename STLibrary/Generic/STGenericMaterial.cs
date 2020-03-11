﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Toolbox.Library
{
    /// <summary>
    /// Represents a generic material used for aa <see cref="STGenericMesh"/>.
    /// This can be used for rendering, exporting, and editing for generic meshes.
    /// </summary>
    public class STGenericMaterial
    {
        /// <summary>
        /// Gets or sets the name of the material.
        /// </summary>
        public string Name { get; set; }

        public List<STGenericTextureMap> TextureMaps = new List<STGenericTextureMap>();
    }
}   