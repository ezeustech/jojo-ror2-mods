using System.Collections.Generic;
namespace Diavolo
{
    // Distance attenuation can create many versions of a long voice clip.
    // Bound the retained bytes, rather than just the number of clip names.
    internal sealed class PcmCache
    {
        private sealed class Entry { internal string Key; internal byte[] Data; }
        private readonly long limit;
        private readonly Dictionary<string,LinkedListNode<Entry>> entries=new Dictionary<string,LinkedListNode<Entry>>();
        private readonly LinkedList<Entry> recent=new LinkedList<Entry>();
        internal long Bytes { get; private set; }
        internal PcmCache(long limit){this.limit=limit;}
        internal bool TryGet(string key,out byte[] data){
            LinkedListNode<Entry> node;
            if(!entries.TryGetValue(key,out node)){data=null;return false;}
            recent.Remove(node);recent.AddFirst(node);data=node.Value.Data;return true;
        }
        internal void Put(string key,byte[] data){
            LinkedListNode<Entry> old;
            if(entries.TryGetValue(key,out old)){Bytes-=old.Value.Data.LongLength;recent.Remove(old);entries.Remove(key);}
            if(data.LongLength>limit)return;
            while(Bytes+data.LongLength>limit && recent.Last!=null){
                var last=recent.Last;Bytes-=last.Value.Data.LongLength;entries.Remove(last.Value.Key);recent.RemoveLast();
            }
            entries[key]=recent.AddFirst(new Entry{Key=key,Data=data});Bytes+=data.LongLength;
        }
    }
}
