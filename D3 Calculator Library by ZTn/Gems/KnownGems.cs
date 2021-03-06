﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ZTn.BNet.D3.Calculator.Helpers;
using ZTn.BNet.D3.Helpers;
using ZTn.BNet.D3.Items;

namespace ZTn.BNet.D3.Calculator.Gems
{
    [DataContract]
    public class KnownGems
    {
        #region >> Fields

        [DataMember(Name = "gems")]
        public List<Item> Gems;

        [IgnoreDataMember]
        private List<Item> helmSocketedGems;
        [IgnoreDataMember]
        private List<Item> otherSocketedGems;
        [IgnoreDataMember]
        private List<Item> weaponSocketedGems;

        #endregion

        #region >> Properties

        [IgnoreDataMember]
        public List<Item> HelmSocketedGems
        {
            get
            {
                if (helmSocketedGems == null)
                {
                    helmSocketedGems = FilterGems("Helm");
                }
                return helmSocketedGems;
            }
        }

        [IgnoreDataMember]
        public List<Item> OtherSocketedGems
        {
            get
            {
                if (otherSocketedGems == null)
                {
                    otherSocketedGems = FilterGems("Other");
                }
                return otherSocketedGems;
            }
        }

        [IgnoreDataMember]
        public List<Item> WeaponSocketedGems
        {
            get
            {
                if (weaponSocketedGems == null)
                {
                    weaponSocketedGems = FilterGems("Weapon");
                }
                return weaponSocketedGems;
            }
        }

        #endregion

        #region >> Constructors

        public KnownGems(List<Item> gems)
        {
            Gems = gems;
        }

        #endregion

        public static KnownGems GetKnownGemsFromJsonFile(String fileName)
        {
            return new KnownGems(fileName.CreateFromJsonFile<List<Item>>());
        }

        public static KnownGems GetKnownGemsFromJsonStream(Stream stream)
        {
            return new KnownGems(stream.CreateFromJsonStream<List<Item>>());
        }

        private List<Item> FilterGems(String itemTypeId)
        {
            var filteredGems = new List<Item>();
            foreach (var gem in Gems)
            {
                filteredGems.AddRange(gem.SocketEffects
                   .Where(e => e.ItemTypeId == itemTypeId)
                   .Select(e => new Item
                   {
                       Id = gem.Id,
                       Attributes = e.Attributes,
                       AttributesRaw = e.AttributesRaw,
                       Name = gem.Name,
                       Icon = gem.Icon
                   }));
            }
            return filteredGems;
        }

        public List<Item> GetGemsForItem(Item item)
        {
            return GetGemsForItemTypeId(item.Type.Id);
        }

        public List<Item> GetGemsForItemTypeId(String itemTypeId)
        {
            if (ItemHelper.WeaponTypeIds.Any(id => itemTypeId == id))
            {
                return WeaponSocketedGems;
            }

            if (ItemHelper.HelmTypeIds.Any(itemTypeId.Contains))
            {
                return HelmSocketedGems;
            }

            return OtherSocketedGems;
        }
    }
}
