﻿using AFSLib;
using Newtonsoft.Json;
using System;
using System.IO;

namespace AFSPacker
{
    // AFSPacker 2.1.1

    class AFSMetadata
    {
        public uint MetadataVersion { get; set; }

        public HeaderMagicType HeaderMagicType { get; set; }
        public AttributesInfoType AttributesInfoType { get; set; }
        public uint EntryBlockAlignment { get; set; }
        public AFSMetadataEntry[] Entries { get; set; }

        const uint CURRENT_VERSION = 2;

        public AFSMetadata()
        {

        }

        public AFSMetadata(AFS afs)
        {
            if (afs == null)
            {
                throw new ArgumentNullException(nameof(afs));
            }

            MetadataVersion = CURRENT_VERSION;

            HeaderMagicType = afs.HeaderMagicType;
            AttributesInfoType = afs.AttributesInfoType;
            EntryBlockAlignment = afs.EntryBlockAlignment;
            Entries = new AFSMetadataEntry[afs.EntryCount];

            for (int e = 0; e < afs.EntryCount; e++)
            {
                if (afs.Entries[e] is NullEntry)
                {
                    Entries[e] = new AFSMetadataEntry()
                    {
                        IsNull = true,
                        Name = string.Empty,
                        FileName = string.Empty,
                        HasUnknownAttribute = false,
                        UnknownAttribute = 0
                    };
                }
                else
                {
                    DataEntry dataEntry = afs.Entries[e] as DataEntry;
                    bool hasUnknownAttribute = afs.ContainsAttributes && dataEntry.HasUnknownAttribute;

                    Entries[e] = new AFSMetadataEntry()
                    {
                        IsNull = false,
                        Name = dataEntry.Name,
                        FileName = dataEntry.SanitizedName,
                        HasUnknownAttribute = hasUnknownAttribute,
                        UnknownAttribute = hasUnknownAttribute ? dataEntry.UnknownAttribute : 0
                    };
                }
            }
        }

        public void SaveToFile(string metadataFileNamePath)
        {
            string metadataContents = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(metadataFileNamePath, metadataContents);
        }

        public static AFSMetadata LoadFromFile(string metadataFileNamePath)
        {
            if (string.IsNullOrEmpty(metadataFileNamePath))
            {
                throw new ArgumentNullException(nameof(metadataFileNamePath));
            }

            string metadataContents = File.ReadAllText(metadataFileNamePath);
            AFSMetadata metadata = JsonConvert.DeserializeObject<AFSMetadata>(metadataContents);

            bool hasBeenMigrated = metadata.Migrate();

            if (hasBeenMigrated)
            {
                metadata.SaveToFile(metadataFileNamePath);
            }

            return metadata;
        }

        #region Metadata Migration

        Action[] migrationMethods;

        private bool Migrate()
        {
            if (MetadataVersion == CURRENT_VERSION) return false;

            if (migrationMethods == null)
            {
                migrationMethods = new Action[]
                {
                    Migrate_1_2
                };
            }

            for (uint v = MetadataVersion - 1; v < CURRENT_VERSION - 1; v++)
            {
                migrationMethods[v]();
            }

            MetadataVersion = CURRENT_VERSION;

            return true;
        }

        /// <summary>
        /// Added EntryBlockAlignment and NullEntry support.
        /// Added HasUnknownAttribute and UnknownAttribute properties.
        /// Renamed Name to FileName and RawName to Name.
        /// </summary>
        void Migrate_1_2()
        {
            EntryBlockAlignment = 0x800;

            for (int e = 0; e < Entries.Length; e++)
            {
                Entries[e].IsNull = false;
                Entries[e].FileName = Entries[e].Name;
                Entries[e].Name = Entries[e].RawName;
                Entries[e].RawName = null;
                Entries[e].HasUnknownAttribute = false;
                Entries[e].UnknownAttribute = 0;
            }
        }

        #endregion
    }
}