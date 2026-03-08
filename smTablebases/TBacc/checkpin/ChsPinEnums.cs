using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBacc
{



	/// <summary>
	/// Every line has two LineTypes. One Left and the other one right of the king. Words "Left" and "Right" are used but lines can be also vertical and diagonal
	/// </summary>
	public enum LineType
	{
		StmK_Empty,
		StmK_Empty_SntmCheckPiece_Unknown,
		StmK_Empty_StmPiece_Empty,
		StmK_Empty_StmPiece_Empty_SntmNonCheckOrStmPiece_Unknown,
		StmK_Empty_StmPiece_Empty_SntmCheckPiece_Unknown,
		StmK_Empty_SntmNonCheckPiece_Unknown
	}
}
