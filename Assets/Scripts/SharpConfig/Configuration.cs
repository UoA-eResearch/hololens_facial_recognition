// Copyright (c) 2013-2016 Cemalettin Dervis, MIT License.
// https://github.com/cemdervis/SharpConfig

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpConfig
{
    /// <summary>
    /// Represents a configuration.
    /// Configurations contain one or multiple sections
    /// that in turn can contain one or multiple settings.
    /// The <see cref="Configuration"/> class is designed
    /// to work with classic configuration formats such as
    /// .ini and .cfg, but is not limited to these.
    /// </summary>
    public partial class Configuration : IEnumerable<Section>
    {
        #region Fields

        private static NumberFormatInfo mNumberFormat;
        private static DateTimeFormatInfo mDateTimeFormat;
        private static char[] mValidCommentChars;
        private static char mArrayElementSeparator;
        private static ITypeStringConverter mFallbackConverter;
        private static Dictionary<Type, ITypeStringConverter> mTypeStringConverters;

        private List<Section> mSections;

        #endregion

        #region Construction

        static Configuration()
        {
            mNumberFormat = CultureInfo.InvariantCulture.NumberFormat;
            mDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            mValidCommentChars = new[] { '#', ';', '\'' };
            mArrayElementSeparator = ',';

            mFallbackConverter = new FallbackStringConverter();
            mTypeStringConverters = new Dictionary<Type, ITypeStringConverter>()
            {
                { typeof(bool), new BoolStringConverter() },
                { typeof(byte), new ByteStringConverter() },
                { typeof(char), new CharStringConverter() },
                { typeof(DateTime), new DateTimeStringConverter() },
                { typeof(decimal), new DecimalStringConverter() },
                { typeof(double), new DoubleStringConverter() },
                { typeof(Enum), new EnumStringConverter() },
                { typeof(short), new Int16StringConverter() },
                { typeof(int), new Int32StringConverter() },
                { typeof(long), new Int64StringConverter() },
                { typeof(sbyte), new SByteStringConverter() },
                { typeof(float), new SingleStringConverter() },
                { typeof(string), new StringStringConverter() },
                { typeof(ushort), new UInt16StringConverter() },
                { typeof(uint), new UInt32StringConverter() },
                { typeof(ulong), new UInt64StringConverter() }
            };

            IgnoreInlineComments = false;
            IgnorePreComments = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration()
        {
            mSections = new List<Section>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets an enumerator that iterates through the configuration.
        /// </summary>
        public IEnumerator<Section> GetEnumerator()
        {
            return mSections.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator that iterates through the configuration.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a section to the configuration.
        /// </summary>
        /// <param name="section">The section to add.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="section"/> is null.</exception>
        /// <exception cref="ArgumentException">When the section already exists in the configuration.</exception>
        public void Add(Section section)
        {
            if (section == null)
                throw new ArgumentNullException("section");

            if (Contains(section))
                throw new ArgumentException("The specified section already exists in the configuration.");

            mSections.Add(section);
        }

        /// <summary>
        /// Removes a section from the configuration by its name.
        /// If there are multiple sections with the same name, only the first section is removed.
        /// To remove all sections that have the name name, use the RemoveAllNamed() method instead.
        /// </summary>
        /// <param name="sectionName">The case-sensitive name of the section to remove.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="sectionName"/> is null or empty.</exception>
        public void Remove(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException("sectionName");

            var section = FindSection(sectionName);

            if (section == null)
            {
                throw new ArgumentException(string.Format(
                    "A section named '{0}' does not exist in the configuration.",
                    sectionName
                    ));
            }

            Remove(section);
        }

        /// <summary>
        /// Removes all sections that have a specific name.
        /// </summary>
        /// <param name="sectionName">The case-sensitive name of the sections to remove.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="sectionName"/> is null or empty.</exception>
        public void RemoveAllNamed(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException("sectionName");

            for (int i = mSections.Count - 1; i >= 0; i--)
            {
                if (string.Equals(mSections[i].Name, sectionName, StringComparison.OrdinalIgnoreCase))
                    mSections.RemoveAt(i);
            }
        }

        /// <summary>
        /// Removes a section from the configuration.
        /// </summary>
        /// <param name="section">The section to remove.</param>
        public void Remove(Section section)
        {
            if (section == null)
                throw new ArgumentNullException("section");

            if (!Contains(section))
                throw new ArgumentException("The specified section does not exist in the section.");

            mSections.Remove(section);
        }

        /// <summary>
        /// Clears the configuration of all sections.
        /// </summary>
        public void Clear()
        {
            mSections.Clear();
        }

        /// <summary>
        /// Determines whether a specified section is contained in the configuration.
        /// </summary>
        /// <param name="section">The section to check for containment.</param>
        /// <returns>True if the section is contained in the configuration; false otherwise.</returns>
        public bool Contains(Section section)
        {
            return mSections.Contains(section);
        }

        /// <summary>
        /// Determines whether a specifically named setting is contained in the section.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>True if the setting is contained in the section; false otherwise.</returns>
        public bool Contains(string sectionName)
        {
            return FindSection(sectionName) != null;
        }

        /// <summary>
        /// Registers a type converter to be used for setting value conversions.
        /// </summary>
        /// <param name="converter">The converter to register.</param>
        public static void RegisterTypeStringConverter(ITypeStringConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException("converter");

            var type = converter.ConvertibleType;
            if (mTypeStringConverters.ContainsKey(type))
                throw new InvalidOperationException(string.Format("A converter for type '{0}' is already registered.", type.FullName));
            else
                mTypeStringConverters.Add(type, converter);
        }

        /// <summary>
        /// Deregisters a type converter from setting value conversion.
        /// </summary>
        /// <param name="type">The type whose associated converter to deregister.</param>
        public static void DeregisterTypeStringConverter(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (!mTypeStringConverters.ContainsKey(type))
                throw new InvalidOperationException(string.Format("No converter is registered for type '{0}'.", type.FullName));
            else
                mTypeStringConverters.Remove(type);
        }

        internal static ITypeStringConverter FindTypeStringConverter(Type type)
        {
            ITypeStringConverter converter = null;
            if (!mTypeStringConverters.TryGetValue(type, out converter))
                converter = mFallbackConverter;

            return converter;
        }

        internal static ITypeStringConverter FallbackConverter
        {
            get { return mFallbackConverter; }
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads a configuration from a file auto-detecting the encoding and
        /// using the default parsing settings.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromFile(string filename)
        {
            return LoadFromFile(filename, null);
        }

        /// <summary>
        /// Loads a configuration from a file.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="encoding">The encoding applied to the contents of the file. Specify null to auto-detect the encoding.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        /// 
        /// <exception cref="ArgumentNullException">When <paramref name="filename"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">When the specified configuration file is not found.</exception>
        public static Configuration LoadFromFile(string filename, Encoding encoding)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            if (!File.Exists(filename))
                throw new FileNotFoundException("Configuration file not found.", filename);

            return (encoding == null) ?
                LoadFromString(File.ReadAllText(filename)) :
                LoadFromString(File.ReadAllText(filename, encoding));
        }

        /// <summary>
        /// Loads a configuration from a text stream auto-detecting the encoding and
        /// using the default parsing settings.
        /// </summary>
        ///
        /// <param name="stream">The text stream to load the configuration from.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromStream(Stream stream)
        {
            return LoadFromStream(stream, null);
        }

        /// <summary>
        /// Loads a configuration from a text stream.
        /// </summary>
        ///
        /// <param name="stream">   The text stream to load the configuration from.</param>
        /// <param name="encoding"> The encoding applied to the contents of the stream. Specify null to auto-detect the encoding.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        /// 
        /// <exception cref="ArgumentNullException">When <paramref name="stream"/> is null.</exception>
        public static Configuration LoadFromStream(Stream stream, Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            string source = null;

            var reader = (encoding == null) ?
                new StreamReader(stream) :
                new StreamReader(stream, encoding);

            using (reader)
                source = reader.ReadToEnd();

            return LoadFromString(source);
        }

        /// <summary>
        /// Loads a configuration from text (source code).
        /// </summary>
        ///
        /// <param name="source">The text (source code) of the configuration.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        /// 
        /// <exception cref="ArgumentNullException">When <paramref name="source"/> is null.</exception>
        public static Configuration LoadFromString(string source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return Parse(source);
        }

        #endregion

        #region LoadBinary

        /// <summary>
        /// Loads a configuration from a binary file using the <b>default</b> <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        /// 
        /// <exception cref="ArgumentNullException">When <paramref name="filename"/> is null or empty.</exception>
        public static Configuration LoadFromBinaryFile(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            return DeserializeBinary(null, filename);
        }

        /// <summary>
        /// Loads a configuration from a binary file using a specific <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="reader">  The reader to use. Specify null to use the default <see cref="BinaryReader"/>.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        /// 
        /// <exception cref="ArgumentNullException">When <paramref name="filename"/> is null or empty.</exception>
        public static Configuration LoadFromBinaryFile(string filename, BinaryReader reader)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            return DeserializeBinary(reader, filename);
        }

        /// <summary>
        /// Loads a configuration from a binary stream, using the <b>default</b> <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to load the configuration from.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        /// 
        /// <exception cref="ArgumentNullException">When <paramref name="stream"/> is null.</exception>
        public static Configuration LoadFromBinaryStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return DeserializeBinary(null, stream);
        }

        /// <summary>
        /// Loads a configuration from a binary stream, using a specific <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to load the configuration from.</param>
        /// <param name="reader">The reader to use. Specify null to use the default <see cref="BinaryReader"/>.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        /// 
        /// <exception cref="ArgumentNullException">When <paramref name="stream"/> is null.</exception>
        public static Configuration LoadFromBinaryStream(Stream stream, BinaryReader reader)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return DeserializeBinary(reader, stream);
        }

        #endregion

        #region Save

        /// <summary>
        /// Saves the configuration to a file using the default character encoding, which is UTF8.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        public void SaveToFile(string filename)
        {
            SaveToFile(filename, null);
        }

        /// <summary>
        /// Saves the configuration to a file.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="encoding">The character encoding to use. Specify null to use the default encoding, which is UTF8.</param>
        public void SaveToFile(string filename, Encoding encoding)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            Serialize(filename, encoding);
        }

        /// <summary>
        /// Saves the configuration to a stream using the default character encoding, which is UTF8.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        public void SaveToStream(Stream stream)
        {
            SaveToStream(stream, null);
        }

        /// <summary>
        /// Saves the configuration to a stream.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        /// <param name="encoding">The character encoding to use. Specify null to use the default encoding, which is UTF8.</param>
        public void SaveToStream(Stream stream, Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            Serialize(stream, encoding);
        }

        #endregion

        #region SaveBinary

        /// <summary>
        /// Saves the configuration to a binary file, using the default <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        public void SaveToBinaryFile(string filename)
        {
            SaveToBinaryFile(filename, null);
        }

        /// <summary>
        /// Saves the configuration to a binary file, using a specific <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="writer">  The writer to use. Specify null to use the default writer.</param>
        public void SaveToBinaryFile(string filename, BinaryWriter writer)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");

            SerializeBinary(writer, filename);
        }

        /// <summary>
        /// Saves the configuration to a binary stream, using the default <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        public void SaveToBinaryStream(Stream stream)
        {
            SaveToBinaryStream(stream, null);
        }

        /// <summary>
        /// Saves the configuration to a binary file, using a specific <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        /// <param name="writer">The writer to use. Specify null to use the default writer.</param>
        public void SaveToBinaryStream(Stream stream, BinaryWriter writer)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            SerializeBinary(writer, stream);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the number format that is used for value conversion in SharpConfig.
        /// The default value is CultureInfo.InvariantCulture.NumberFormat.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException">When a null reference is set as the value.</exception>
        public static NumberFormatInfo NumberFormat
        {
            get { return mNumberFormat; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                mNumberFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the DateTime format that is used for value conversion in SharpConfig.
        /// The default value is CultureInfo.InvariantCulture.DateTimeFormat.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException">When a null reference is set as the value.</exception>
        public static DateTimeFormatInfo DateTimeFormat
        {
            get { return mDateTimeFormat; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                mDateTimeFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the array that contains all comment delimiting characters.
        /// </summary>
        /// 
        /// <exception cref="ArgumentNullException">When a null reference is set as the value.</exception>
        /// <exception cref="ArgumentException">When an empty array is set as the value.</exception>
        public static char[] ValidCommentChars
        {
            get { return mValidCommentChars; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Length == 0)
                {
                    throw new ArgumentException(
                        "The comment chars array must not be empty.",
                        "value");
                }

                mValidCommentChars = value;
            }
        }

        /// <summary>
        /// Gets or sets the array element separator character for settings.
        /// The default value is ','.
        /// NOTE: remember that after you change this value while <see cref="Setting"/> instances exist,
        /// to expect their ArraySize and other array-related values to return different values.
        /// </summary>
        /// 
        /// <exception cref="ArgumentException">When a zero-character ('\0') is set as the value.</exception>
        public static char ArrayElementSeparator
        {
            get { return mArrayElementSeparator; }
            set
            {
                if (value == '\0')
                    throw new ArgumentException("Zero-character is not allowed.");

                mArrayElementSeparator = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether inline-comments
        /// should be ignored when parsing a configuration.
        /// </summary>
        public static bool IgnoreInlineComments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether pre-comments
        /// should be ignored when parsing a configuration.
        /// </summary>
        public static bool IgnorePreComments { get; set; }

        /// <summary>
        /// Gets the number of sections that are in the configuration.
        /// </summary>
        public int SectionCount
        {
            get { return mSections.Count; }
        }

        /// <summary>
        /// Gets or sets a section by index.
        /// </summary>
        /// <param name="index">The index of the section in the configuration.</param>
        /// 
        /// <returns>
        /// The section at the specified index.
        /// Note: no section is created when using this accessor.
        /// </returns>
        /// 
        /// <exception cref="ArgumentOutOfRangeException">When the index is out of range.</exception>
        public Section this[int index]
        {
            get
            {
                if (index < 0 || index >= mSections.Count)
                    throw new ArgumentOutOfRangeException("index");

                return mSections[index];
            }
        }

        /// <summary>
        /// Gets or sets a section by its name.
        /// If there are multiple sections with the same name, the first section is returned.
        /// If you want to obtain all sections that have the same name, use the GetSectionsNamed() method instead.
        /// </summary>
        ///
        /// <param name="name">The case-sensitive name of the section.</param>
        ///
        /// <returns>
        /// The section if found, otherwise a new section with
        /// the specified name is created, added to the configuration and returned.
        /// </returns>
        public Section this[string name]
        {
            get
            {
                var section = FindSection(name);

                if (section == null)
                {
                    section = new Section(name);
                    Add(section);
                }

                return section;
            }
        }

        /// <summary>
        /// Gets all sections that have a specific name.
        /// </summary>
        /// <param name="name">The case-sensitive name of the sections.</param>
        /// <returns>
        /// The found sections.
        /// </returns>
        public IEnumerable<Section> GetSectionsNamed(string name)
        {
            var sections = new List<Section>();

            foreach (var section in mSections)
            {
                if (string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase))
                    sections.Add(section);
            }

            return sections;
        }

        // Finds a section by its name.
        private Section FindSection(string name)
        {
            foreach (var section in mSections)
            {
                if (string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase))
                    return section;
            }

            return null;
        }

        #endregion
    }
}