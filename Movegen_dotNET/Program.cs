// ==++==
//
//   Daniel Infuehr (c) All rights reserved.
//
// ==--==
/*============================================================
**
** Class:  Program.cs
**
** Purpose: Comparison of Sliding Piece Algorithms in dotNet vs c++
**
**
===========================================================*/

using Movegen;

AlgorithmCollection collection = new();
Console.WriteLine(Runtime.Environment.GetProcessorName());

collection.Verify();
AlgorithmCollection.PrintHeader();
AlgorithmCollection.Run();
