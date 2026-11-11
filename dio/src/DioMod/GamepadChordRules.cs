namespace EyesOfHeavenDio
{
    public static class GamepadChordRules
    {
        public static bool Held(ushort current,ushort mask){return mask!=0 && (current&mask)==mask;}
        public static bool Pressed(ushort current,ushort previous,ushort mask){
            if((current&192)==192 && mask!=192)return false;
            return Held(current,mask) && !Held(previous,mask) && ((mask&128)!=0 || (current&128)==0);
        }
        public static bool Held(ushort current,ushort mask,ushort[] bindings,ushort priority=0){
            if((current&128)!=0 && (mask&128)==0)return false;
            if(mask!=priority && Held(current,priority))return false;
            if(!Held(current,mask))return false;
            foreach(ushort other in bindings)
                if(other!=mask && (other&mask)==mask && Held(current,other))return false;
            return true;
        }
        public static bool Pressed(ushort current,ushort previous,ushort mask,ushort[] bindings,ushort priority=0){
            return Held(current,mask,bindings,priority) && !Held(previous,mask);
        }
    }
}
