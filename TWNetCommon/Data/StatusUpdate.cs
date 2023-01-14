﻿using MessagePack;

namespace TWNetCommon.Data
{
    [MessagePackObject()]
    public class StatusUpdate
    {
        [Key(0)]
        public string UserID { get; set; }
        [Key(1)]
        public bool EnableStatus { get; set; }
        [Key(2)]
        public bool DisplaySpecialRank { get; set; }
        [Key(3)]
        public string SpecialRank { get; set; }
        [Key(4)]
        public string SpecialRankColour { get; set; }
        [Key(5)]
        public string SpecialRankTextColour { get; set; }
        [Key(6)]
        public bool IsLookingForGroup { get; set; } //Based off if an auto accept is enabled

        public override string ToString()
        {
            return $"Status Update - [UserID: {UserID}, EnableStatus: {EnableStatus}, DisplaySpecialRank: {DisplaySpecialRank}, SpecialRank: {SpecialRank}, SpecialRankColour: {SpecialRankColour}, SpecialRankTextColour: {SpecialRankTextColour}, IsLookingForGroup: {IsLookingForGroup}]";
        }
    }
}