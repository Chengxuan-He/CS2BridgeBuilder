// Offline native method projection: only container types and visibility changed.
// Source: artifacts/preview-markings/native/NetCompositionHelpers.cs
using Game.Prefabs;
using Colossal.Mathematics;
using Unity.Mathematics;
internal static class NativePacking {
	internal static void Pack(ref NetCompositionData compositionData, NetCompositionPiece[] pieces, System.Collections.Generic.Dictionary<Unity.Entities.Entity, NetPieceData> netPieceData)
	{
		compositionData.m_Width = 0f;
		compositionData.m_MiddleOffset = 0f;
		compositionData.m_WidthOffset = 0f;
		compositionData.m_NodeOffset = 0f;
		compositionData.m_HeightRange = new Bounds1(float.MaxValue, float.MinValue);
		compositionData.m_SurfaceHeight = new Bounds1(float.MaxValue, float.MinValue);
		bool test = (compositionData.m_Flags.m_General & CompositionFlags.General.Invert) != 0;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		int num8 = 0;
		while (num8 < pieces.Length)
		{
			NetCompositionPiece value = pieces[num8];
			bool flag5 = (value.m_SectionFlags & NetSectionFlags.Underground) != 0;
			bool flag6 = (value.m_SectionFlags & NetSectionFlags.Overhead) != 0;
			bool test2 = (value.m_SectionFlags & NetSectionFlags.Invert) != 0;
			bool test3 = (value.m_SectionFlags & NetSectionFlags.FlipMesh) != 0;
			bool2 test4 = new bool2((value.m_SectionFlags & NetSectionFlags.Left) != 0, (value.m_SectionFlags & NetSectionFlags.Right) != 0);
			NetPieceData netPieceData2 = netPieceData[value.m_Piece];
			float num9 = netPieceData2.m_Width;
			if (!flag5 || (compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) == 0)
			{
				compositionData.m_HeightRange |= value.m_Offset.y + netPieceData2.m_HeightRange;
			}
			if (!flag5 && !flag6 && (value.m_PieceFlags & NetPieceFlags.PreserveShape) == 0)
			{
				if ((value.m_PieceFlags & NetPieceFlags.Side) != 0)
				{
					float4 falseValue = math.select(netPieceData2.m_SurfaceHeights, netPieceData2.m_SurfaceHeights.yxwz, test2);
					falseValue = math.select(falseValue, falseValue.zwxy, test3);
					compositionData.m_EdgeHeights = math.select(compositionData.m_EdgeHeights, falseValue, test4.xyxy);
					compositionData.m_SideConnectionOffset = math.select(compositionData.m_SideConnectionOffset, netPieceData2.m_SideConnectionOffset, test4);
				}
				if ((value.m_PieceFlags & NetPieceFlags.Surface) != 0)
				{
					compositionData.m_SurfaceHeight.min = math.min(compositionData.m_SurfaceHeight.min, value.m_Offset.y + math.cmin(netPieceData2.m_SurfaceHeights));
					compositionData.m_SurfaceHeight.max = math.max(compositionData.m_SurfaceHeight.max, value.m_Offset.y + math.cmax(netPieceData2.m_SurfaceHeights));
					flag = true;
				}
			}
			compositionData.m_WidthOffset = math.max(compositionData.m_WidthOffset, netPieceData2.m_WidthOffset);
			compositionData.m_NodeOffset = math.max(compositionData.m_NodeOffset, netPieceData2.m_NodeOffset);
			value.m_Size.x = netPieceData2.m_Width;
			value.m_Size.y = netPieceData2.m_HeightRange.max - netPieceData2.m_HeightRange.min;
			value.m_Size.z = netPieceData2.m_Length;
			int i;
			for (i = num8 + 1; i < pieces.Length; i++)
			{
				NetCompositionPiece value2 = pieces[i];
				if (value2.m_SectionIndex != value.m_SectionIndex)
				{
					break;
				}
				NetPieceData netPieceData3 = netPieceData[value2.m_Piece];
				num9 = math.max(num9, netPieceData3.m_Width);
				if (!flag5 || (compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) == 0)
				{
					compositionData.m_HeightRange |= value2.m_Offset.y + netPieceData3.m_HeightRange;
				}
				if (!flag5 && !flag6 && (value2.m_PieceFlags & NetPieceFlags.PreserveShape) == 0)
				{
					if ((value2.m_PieceFlags & NetPieceFlags.Side) != 0)
					{
						float4 falseValue2 = math.select(netPieceData3.m_SurfaceHeights, netPieceData3.m_SurfaceHeights.yxwz, test2);
						falseValue2 = math.select(falseValue2, falseValue2.zwxy, test3);
						compositionData.m_EdgeHeights = math.select(compositionData.m_EdgeHeights, falseValue2, test4.xyxy);
					}
					if ((value2.m_PieceFlags & NetPieceFlags.Surface) != 0)
					{
						compositionData.m_SurfaceHeight.min = math.min(compositionData.m_SurfaceHeight.min, value2.m_Offset.y + math.cmin(netPieceData3.m_SurfaceHeights));
						compositionData.m_SurfaceHeight.max = math.max(compositionData.m_SurfaceHeight.max, value2.m_Offset.y + math.cmax(netPieceData3.m_SurfaceHeights));
						flag = true;
					}
				}
				compositionData.m_WidthOffset = math.max(compositionData.m_WidthOffset, netPieceData3.m_WidthOffset);
				compositionData.m_NodeOffset = math.max(compositionData.m_NodeOffset, netPieceData3.m_NodeOffset);
				value2.m_Size.x = netPieceData3.m_Width;
				value2.m_Size.y = netPieceData3.m_HeightRange.max - netPieceData3.m_HeightRange.min;
				value2.m_Size.z = netPieceData3.m_Length;
				pieces[i] = value2;
			}
			float x = value.m_Offset.x;
			if (flag5)
			{
				value.m_Offset.x += num + num9 * 0.5f;
				num += num9;
				if ((value.m_SectionFlags & (NetSectionFlags.Median | NetSectionFlags.AlignCenter)) == NetSectionFlags.Median)
				{
					num2 = value.m_Offset.x - math.select(x * 2f, 0f, test);
					num3 = x;
					flag3 = true;
				}
				else if ((value.m_SectionFlags & (NetSectionFlags.Right | NetSectionFlags.AlignCenter)) == NetSectionFlags.Right && !flag3)
				{
					num2 = value.m_Offset.x - value.m_Size.x * 0.5f - math.select(x * 2f, 0f, test);
					num3 = x;
					flag3 = true;
				}
			}
			else if (flag6)
			{
				value.m_Offset.x += num4 + num9 * 0.5f;
				num4 += num9;
				if ((value.m_SectionFlags & (NetSectionFlags.Median | NetSectionFlags.AlignCenter)) == NetSectionFlags.Median)
				{
					num5 = value.m_Offset.x - math.select(x * 2f, 0f, test);
					num6 = x;
					flag4 = true;
				}
				else if ((value.m_SectionFlags & (NetSectionFlags.Right | NetSectionFlags.AlignCenter)) == NetSectionFlags.Right && !flag4)
				{
					num5 = value.m_Offset.x - value.m_Size.x * 0.5f - math.select(x * 2f, 0f, test);
					num6 = x;
					flag4 = true;
				}
			}
			else
			{
				value.m_Offset.x += compositionData.m_Width + num9 * 0.5f;
				compositionData.m_Width += num9;
				if ((value.m_SectionFlags & (NetSectionFlags.Median | NetSectionFlags.AlignCenter)) == NetSectionFlags.Median)
				{
					compositionData.m_MiddleOffset = value.m_Offset.x - math.select(x * 2f, 0f, test);
					num7 = x;
					flag2 = true;
				}
				else if ((value.m_SectionFlags & (NetSectionFlags.Right | NetSectionFlags.AlignCenter)) == NetSectionFlags.Right && !flag2)
				{
					compositionData.m_MiddleOffset = value.m_Offset.x - value.m_Size.x * 0.5f - math.select(x * 2f, 0f, test);
					num7 = x;
					flag2 = true;
				}
			}
			pieces[num8] = value;
			for (int j = num8 + 1; j < i; j++)
			{
				NetCompositionPiece value3 = pieces[j];
				value3.m_Offset.x = value.m_Offset.x;
				pieces[j] = value3;
			}
			if ((value.m_PieceFlags & NetPieceFlags.Side) != 0 && i > num8 + 1 && (value.m_SectionFlags & (NetSectionFlags.Left | NetSectionFlags.Right)) != 0)
			{
				bool test5 = (value.m_SectionFlags & NetSectionFlags.Right) != 0;
				for (int k = num8; k < i; k++)
				{
					NetCompositionPiece value4 = pieces[k];
					float num10 = (netPieceData[value4.m_Piece].m_Width - num9) * 0.5f;
					value4.m_Offset.x += math.select(0f - num10, num10, test5);
					pieces[k] = value4;
				}
			}
			num8 = i;
		}
		if (flag3)
		{
			num2 -= num * 0.5f;
		}
		if (flag4)
		{
			num5 -= num4 * 0.5f;
		}
		if (flag2)
		{
			compositionData.m_MiddleOffset -= compositionData.m_Width * 0.5f;
		}
		if ((compositionData.m_Flags.m_General & (CompositionFlags.General.DeadEnd | CompositionFlags.General.LevelCrossing)) == CompositionFlags.General.LevelCrossing || (compositionData.m_Flags.m_General & (CompositionFlags.General.DeadEnd | CompositionFlags.General.Intersection | CompositionFlags.General.Crosswalk)) == CompositionFlags.General.Crosswalk || (compositionData.m_Flags.m_Right & CompositionFlags.Side.AbruptEnd) != 0)
		{
			compositionData.m_State |= CompositionState.BlockUTurn;
		}
		CompositionState compositionState = (CompositionState)0;
		for (int l = 0; l < pieces.Length; l++)
		{
			NetCompositionPiece value5 = pieces[l];
			bool flag7 = (value5.m_SectionFlags & NetSectionFlags.Underground) != 0;
			bool flag8 = (value5.m_SectionFlags & NetSectionFlags.Overhead) != 0;
			if ((value5.m_PieceFlags & (NetPieceFlags.PreserveShape | NetPieceFlags.BlockTraffic)) == NetPieceFlags.BlockTraffic)
			{
				compositionData.m_State |= CompositionState.BlockUTurn;
			}
			if ((value5.m_PieceFlags & NetPieceFlags.LowerBottomToTerrain) != 0)
			{
				compositionData.m_State |= CompositionState.LowerToTerrain;
			}
			if ((value5.m_PieceFlags & NetPieceFlags.RaiseTopToTerrain) != 0)
			{
				compositionData.m_State |= CompositionState.RaiseToTerrain;
			}
			if ((value5.m_SectionFlags & NetSectionFlags.HalfLength) != 0)
			{
				compositionData.m_State |= CompositionState.HalfLength;
			}
			if ((value5.m_SectionFlags & NetSectionFlags.Hidden) != 0)
			{
				compositionData.m_State |= CompositionState.Hidden;
			}
			else if (!flag7 && !flag8)
			{
				compositionState |= CompositionState.Hidden;
			}
			if (flag7)
			{
				value5.m_Offset.x -= num * 0.5f + num3;
				value5.m_Offset.x += compositionData.m_MiddleOffset - num2;
			}
			else if (flag8)
			{
				value5.m_Offset.x -= num4 * 0.5f + num6;
				value5.m_Offset.x += compositionData.m_MiddleOffset - num5;
			}
			else
			{
				value5.m_Offset.x -= compositionData.m_Width * 0.5f + num7;
			}
			pieces[l] = value5;
		}
		compositionData.m_State &= ~compositionState;
		compositionData.m_Width = math.max(compositionData.m_Width, math.max(num, num4));
		if (compositionData.m_HeightRange.min > compositionData.m_HeightRange.max)
		{
			compositionData.m_HeightRange = default(Bounds1);
		}
		if (flag)
		{
			compositionData.m_State |= CompositionState.HasSurface;
			if ((compositionData.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) == 0)
			{
				compositionData.m_State |= CompositionState.ExclusiveGround;
			}
		}
		else
		{
			float num11 = MathUtils.Center(compositionData.m_HeightRange);
			compositionData.m_SurfaceHeight = new Bounds1(num11, num11);
		}
	}

}
