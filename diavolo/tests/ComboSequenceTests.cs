using System;
using Diavolo;
public static class ComboSequenceTests {
 public static void Main(){var c=new ComboSequence();for(int i=0;i<10;i++){if(c.Take(i)!=i%5)throw new Exception("Chain did not cycle five hits");c.Finish(i+.5f,true);}if(c.Take(20)!=0)throw new Exception("Expired chain did not reset");c.Finish(20.1f,false);if(c.Take(20.2f)!=0)throw new Exception("Interrupted windup did not reset");Console.WriteLine("PASS: five-hit chain cycles, timeout reset and interrupted windup reset.");}
}
