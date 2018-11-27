// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.UI.Composition;

namespace Microsoft.Toolkit.Uwp.UI.Animations
{
    /// <summary>
    /// Defines an <see cref="EffectAnimationDefinition"/> which is used by
    /// <see cref="AnimationSet"/> to link effect animations to Visuals
    /// </summary>
    internal class EffectAnimationDefinition
    {
        /// <summary>
        /// Gets or sets <see cref="CompositionObject"/> that will be animated
        /// </summary>
        public CompositionObject EffectBrush { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CompositionAnimation"/>
        /// </summary>
        public CompositionAnimation Animation { get; set; }

        /// <summary>
        /// Gets or sets the property name that will be animated on the <see cref="CompositionEffectBrush"/>
        /// </summary>
        public string PropertyName { get; set; }
    }
}
