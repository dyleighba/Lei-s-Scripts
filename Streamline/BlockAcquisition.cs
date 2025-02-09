using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public struct TaggedBlock
    {
        public IMyTerminalBlock Block;
        public List<string> Tags;
        public string Location;
        public bool IsTaggedBlock;

        public bool HasTag(string tag)
        {
            return Tags.Contains(tag.ToLower());
        }
        
        public TaggedBlock(IMyTerminalBlock block)
        {
            Block = block;
            Tags = new List<string>();
            string fullString = block.CustomName.Trim().ToLower();
            if (fullString[0] != '[')
            {
                IsTaggedBlock = false;
                Location = null;
                return;
            }
            Location = fullString.Substring(1, fullString.IndexOf(']')).Trim();
            foreach (var tag in fullString.Substring(fullString.IndexOf(']') + 1).Split('#'))
            {
                Tags.Add(tag.Trim().ToLower());
            }
            
            IsTaggedBlock = Tags.Count > 0;
        }
        
        public static List<TaggedBlock> GetAllTaggedBlocks(IMyGridTerminalSystem blockSystem)
        {
            List<TaggedBlock> taggedBlocks = new List<TaggedBlock>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blockSystem.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                TaggedBlock taggedBlock = new TaggedBlock(block);
                if (taggedBlock.IsTaggedBlock)
                {
                    taggedBlocks.Add(taggedBlock);
                }
            }
            return taggedBlocks;
        }
        
        public static List<TaggedBlock> GetAllTaggedBlocksByLocation(IMyGridTerminalSystem blockSystem, string location)
        {
            List<TaggedBlock> taggedBlocks = new List<TaggedBlock>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blockSystem.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                TaggedBlock taggedBlock = new TaggedBlock(block);
                if (taggedBlock.IsTaggedBlock && string.Equals(taggedBlock.Location, location, StringComparison.InvariantCultureIgnoreCase))
                {
                    taggedBlocks.Add(taggedBlock);
                }
            }
            return taggedBlocks;
        }
    }
    
    
}