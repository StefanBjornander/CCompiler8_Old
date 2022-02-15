using System.Diagnostics;
using System.Collections.Generic;

namespace CCompiler {
  public class RegisterAllocator {
    public RegisterAllocator(ISet<Track> totalTrackSet,
                             List<AssemblyCode> assemblyCodeList) {
      Graph<Track> totalTrackGraph = new Graph<Track>(totalTrackSet);

      foreach (Track track1 in totalTrackSet) {
        foreach (Track track2 in totalTrackSet) {
          if (!track1.Equals(track2) && Track.Overlaps(track1, track2)) {
            totalTrackGraph.AddEdge(track1, track2);
          }
        }
      }

      ISet<Graph<Track>> split = totalTrackGraph.Split();
      foreach (Graph<Track> trackGraph in split) {
        List<Track> trackList = new List<Track>(trackGraph.VertexSet);
        Error.Check(DeepFirstSearch(trackList, 0, trackGraph),
                    Message.Out_of_registers);
      }
    
      SetRegistersInCodeList(assemblyCodeList);
    }

    private static void SetRegistersInCodeList(List<AssemblyCode> 
                                               assemblyCodeList) {
      foreach (AssemblyCode assemblyCode in assemblyCodeList) {
        if (assemblyCode.Operator == AssemblyOperator.set_track_size) {
          Track track = (Track) assemblyCode[0];
          object operand1 = assemblyCode[1];

          if (operand1 is int) {
            track.CurrentSize = (int) operand1;
          }
          else {
            track.CurrentSize = ((Track) operand1).CurrentSize;
          }

          assemblyCode.Operator = AssemblyOperator.empty;
        }
        else {
          Check(assemblyCode, 0);
          Check(assemblyCode, 1);
          Check(assemblyCode, 2);
        }
      }
    }

    private static void Check(AssemblyCode assemblyCode, int position) {
      if (assemblyCode[position] is Track) {
        Track track = (Track) assemblyCode[position];
        Error.ErrorXXX(track.Register != null);
        assemblyCode[position] =
         AssemblyCode.RegisterToSize(track.Register.Value, track.CurrentSize);
      }
    }

    private bool DeepFirstSearch(List<Track> trackList, int listIndex,
                                 Graph<Track> trackGraph) {
      if (listIndex == trackList.Count) {
        return true;
      }

      Track track = trackList[listIndex];
      if (track.Register != null) {
        return DeepFirstSearch(trackList, listIndex + 1, trackGraph);
      }

      ISet<Register> possibleSet = GetPossibleSet(track);
      ISet<Track> neighbourSet = trackGraph.GetNeighbourSet(track);

      foreach (Register possibleRegister in possibleSet) {
        if (!OverlapNeighbourSet(possibleRegister, neighbourSet)) {
          track.Register = possibleRegister;

          if (DeepFirstSearch(trackList, listIndex + 1, trackGraph)) {
            return true;
          }

          track.Register = null;
        }
      }

      track.Register = null;
      return false;
    }

    private bool OverlapNeighbourSet(Register register,
                                     ISet<Track> neighbourSet) {
      foreach (Track neighbourTrack in neighbourSet) {
        if (AssemblyCode.RegisterOverlap(register, neighbourTrack.Register)) {
          return true;
        }
      }

      return false;
    }

    public static ISet<Register>
      VariadicFunctionPointerRegisterSet = new HashSet<Register>() {
        AssemblyCode.RegisterToSize(Register.bp, TypeSize.PointerSize),
        AssemblyCode.RegisterToSize(Register.si, TypeSize.PointerSize),
        AssemblyCode.RegisterToSize(Register.di, TypeSize.PointerSize),
        AssemblyCode.RegisterToSize(Register.bx, TypeSize.PointerSize)
      },
      RegularFunctionPointerRegisterSet = new HashSet<Register>(VariadicFunctionPointerRegisterSet),
      Byte1RegisterSet = new HashSet<Register>() {
        Register.al,Register.ah, Register.bl, Register.bh, 
        Register.cl, Register.ch, Register.dl, Register.dh
      },
      Byte2RegisterSet = new HashSet<Register>() {
        Register.ax, Register.bx, Register.cx, Register.dx
      };

    static RegisterAllocator() {
      VariadicFunctionPointerRegisterSet.
        Remove(AssemblyCode.RegularFrameRegister);
      RegularFunctionPointerRegisterSet.
        Remove(AssemblyCode.RegularFrameRegister);
      RegularFunctionPointerRegisterSet.
        Remove(AssemblyCode.VariadicFrameRegister);
    }

    private static ISet<Register> GetPossibleSet(Track track) {
      if (track.Pointer) {
        if (SymbolTable.CurrentFunction.Type.IsVariadic()) {
          return RegularFunctionPointerRegisterSet;
        }
        else {
          return VariadicFunctionPointerRegisterSet;
        }
      }
      else if (track.MaxSize == 1) {
        return Byte1RegisterSet;
      }
      else {
        return Byte2RegisterSet;
      }
    }
  }
}