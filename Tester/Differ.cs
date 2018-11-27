using System;
using System.Collections.Generic;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;

namespace Tester {
  public class InlineDiffBuilder : IInlineDiffBuilder
  {
    private readonly IDiffer differ;

    public InlineDiffBuilder(IDiffer differ)
    {
      IDiffer differ1 = differ;
      if (differ1 == null)
        throw new ArgumentNullException(nameof (differ));
      this.differ = differ1;
    }

    public DiffPaneModel BuildDiffModel(string oldText, string newText)
    {
      if (oldText == null)
        throw new ArgumentNullException(nameof (oldText));
      if (newText == null)
        throw new ArgumentNullException(nameof (newText));
      DiffPaneModel diffPaneModel = new DiffPaneModel();
      InlineDiffBuilder.BuildDiffPieces(this.differ.CreateLineDiffs(oldText, newText, false), diffPaneModel.Lines);
      return diffPaneModel;
    }

    private static void BuildDiffPieces(DiffResult diffResult, List<DiffPiece> pieces)
    {
      int index1 = 0;
      foreach (DiffBlock diffBlock in (IEnumerable<DiffBlock>) diffResult.DiffBlocks)
      {
        for (; index1 < diffBlock.InsertStartB; ++index1)
          pieces.Add(new DiffPiece(diffResult.PiecesNew[index1], ChangeType.Unchanged, new int?(index1 + 1)));
        for (int index2 = 0; index2 < Math.Min(diffBlock.DeleteCountA, diffBlock.InsertCountB); ++index2)
          pieces.Add(new DiffPiece(diffResult.PiecesOld[index2 + diffBlock.DeleteStartA], ChangeType.Deleted, new int?()));
        int num;
        for (num = 0; num < Math.Min(diffBlock.DeleteCountA, diffBlock.InsertCountB); ++num)
        {
          pieces.Add(new DiffPiece(diffResult.PiecesNew[num + diffBlock.InsertStartB], ChangeType.Inserted, new int?(index1 + 1)));
          ++index1;
        }
        if (diffBlock.DeleteCountA > diffBlock.InsertCountB)
        {
          for (; num < diffBlock.DeleteCountA; ++num)
            pieces.Add(new DiffPiece(diffResult.PiecesOld[num + diffBlock.DeleteStartA], ChangeType.Deleted, new int?()));
        }
        else
        {
          for (; num < diffBlock.InsertCountB; ++num)
          {
            pieces.Add(new DiffPiece(diffResult.PiecesNew[num + diffBlock.InsertStartB], ChangeType.Inserted, new int?(index1 + 1)));
            ++index1;
          }
        }
      }
      for (; index1 < diffResult.PiecesNew.Length; ++index1)
        pieces.Add(new DiffPiece(diffResult.PiecesNew[index1], ChangeType.Unchanged, new int?(index1 + 1)));
    }
  }
}

