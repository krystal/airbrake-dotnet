//-----------------------------------------------------------------------
// <copyright file="KeyValuePair.cs" company="aTech Media Ltd">
//     Copyright (c) aTech Media Ltd. All rights reserved.
// </copyright>
// <author>Jack Hayter</author>
//-----------------------------------------------------------------------
namespace Airbrake
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A simple key/value pair class used by the Airbrake client for storage of custom parameters.
    /// </summary>
    public class KeyValuePair
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValuePair"/> class.
        /// </summary>
        public KeyValuePair()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValuePair"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public KeyValuePair(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value
        {
            get;
            set;
        }
    }
}
