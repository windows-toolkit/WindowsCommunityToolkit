﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.SampleApp.SamplePages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UserHandPreferenceStateTriggerPage : Page
    {
        private Button _addButton;
        private Button _removeButton;
        private ListBox _listBox;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserHandPreferenceStateTriggerPage"/> class.
        /// </summary>
        public UserHandPreferenceStateTriggerPage()
        {
            InitializeComponent();
        }
    }
}
