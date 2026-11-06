using System;
using System.Collections.Generic;

namespace GrowingCity
{
    public enum RoadShape { End, Straight, Corner, Tee, Cross }
    public readonly struct RoadJunction
    {
        public readonly int Mask, QuarterTurns;
        public readonly RoadShape Shape;
        public RoadJunction(int mask,RoadShape shape,int turns){Mask=mask;Shape=shape;QuarterTurns=turns;}
    }
    public sealed class RoadLayout
    {
        public readonly HashSet<Cell> Horizontal=new HashSet<Cell>();
        public readonly HashSet<Cell> Vertical=new HashSet<Cell>();
        public readonly Dictionary<Cell,RoadJunction> Junctions=new Dictionary<Cell,RoadJunction>();
    }
    public static class RoadTopology
    {
        // Direction bits: north(+Z)=1, east(+X)=2, south=4, west=8.
        // The supplied asset's vehicle path endpoints establish Corner=NW, Tee=NEW, Straight=NS, End=N.
        public static int Rotate(int mask,int turns){turns=((turns%4)+4)%4;return ((mask<<turns)|(mask>>(4-turns)))&15;}
        public static RoadJunction Resolve(int mask)
        {
            if(mask<1||mask>15)throw new ArgumentOutOfRangeException(nameof(mask));
            int count=0;for(int n=mask;n!=0;n>>=1)count+=n&1;
            RoadShape shape=count==4?RoadShape.Cross:count==3?RoadShape.Tee:count==1?RoadShape.End:(mask==5||mask==10)?RoadShape.Straight:RoadShape.Corner;
            int basis=shape==RoadShape.Cross?15:shape==RoadShape.Tee?11:shape==RoadShape.End?1:shape==RoadShape.Straight?5:9;
            for(int turns=0;turns<4;turns++)if(Rotate(basis,turns)==mask)return new RoadJunction(mask,shape,turns);
            throw new InvalidOperationException("Unresolved road mask");
        }
        public static int FloorDivide(int value,int divisor)=> (int)Math.Floor(value/(double)divisor);
        public static RoadLayout Build(IEnumerable<Cell> lots,int blockSize=2,IEnumerable<LotConnection> sharedLinks=null)
        {
            if(blockSize<1)throw new ArgumentOutOfRangeException(nameof(blockSize));
            var layout=new RoadLayout();var nodes=new HashSet<Cell>();
            foreach(var lot in lots){var block=new Cell(FloorDivide(lot.X,blockSize),FloorDivide(lot.Z,blockSize));
                layout.Horizontal.Add(block);layout.Horizontal.Add(block.Offset(0,1));
                layout.Vertical.Add(block);layout.Vertical.Add(block.Offset(1,0));
                nodes.Add(block);nodes.Add(block.Offset(1,0));nodes.Add(block.Offset(0,1));nodes.Add(block.Offset(1,1));
            }
            if(sharedLinks!=null)foreach(var link in sharedLinks){var a=new Cell(FloorDivide(link.A.X,blockSize),FloorDivide(link.A.Z,blockSize));var b=new Cell(FloorDivide(link.B.X,blockSize),FloorDivide(link.B.Z,blockSize));if(a.Equals(b))continue;if(a.X!=b.X)layout.Vertical.Remove(new Cell(Math.Max(a.X,b.X),a.Z));else layout.Horizontal.Remove(new Cell(a.X,Math.Max(a.Z,b.Z)));}
            foreach(var node in nodes){int mask=0;if(layout.Vertical.Contains(node))mask|=1;if(layout.Horizontal.Contains(node))mask|=2;if(layout.Vertical.Contains(node.Offset(0,-1)))mask|=4;if(layout.Horizontal.Contains(node.Offset(-1,0)))mask|=8;if(mask!=0)layout.Junctions.Add(node,Resolve(mask));}
            return layout;
        }
    }
}
