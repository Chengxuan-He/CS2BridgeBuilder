// Measured DrawBridge01Pillar main mesh, native game 1.6.2f1.
// Simplified LOD stations inherit reviewed high-detail wall membership and span.
using System;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw01ColumnWideningData
{
    internal static MovableStructureModel.Plan CreatePlan(int lod, Vector3[] sourcePositions, float halfWidthDelta, int[][] sourceIndices)
    {
        var counts = lod switch
        {
            0 => (Vertices: Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[0], Triangles: Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[1]),
            1 => (Vertices: Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[2], Triangles: Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[3]),
            2 => (Vertices: Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[4], Triangles: Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[5]),
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
        if (sourcePositions.Length != counts.Vertices || sourceIndices.Length != Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[6] || sourceIndices[Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[7]].Length != counts.Triangles * Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.I32[8])
            throw new ArgumentException("Draw01 column requires the recorded native mesh layout.");
        var positions = (Vector3[])sourcePositions.Clone();
        var groups = lod switch
        {
            0 => Near,
            1 => Middle,
            2 => Far,
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
        foreach (var group in groups)
        {
            if (group.Weight == Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values.F32[0])
                continue;
            var displacement = group.Weight * halfWidthDelta;
            AffineGeometry.Indices(group.Vertices, index =>
            {
                positions[index].x = AffineGeometry.Translate(positions[index].x, displacement);
            });
        }

        return new MovableStructureModel.Plan(sourcePositions, positions, sourceIndices, new[] { new bool[counts.Triangles] });
    }

    // Native Geometry SHA-256: 58E287710EBCCC17052472C54F6C88EDFD876DE5C8DFB68BB40BE5236DF4E5B8
    private static readonly (float Weight, int[] Vertices)[] Near = ReadPackedNear();
    // Native Geometry SHA-256: 75FD4EC55594F8B785CF8ABA7D826645BB92FF04BD7AA7FF7B0E3C030DD3BB45
    private static readonly (float Weight, int[] Vertices)[] Middle = ReadPackedMiddle();
    // Native Geometry SHA-256: DC1A8A335221D46CAAF2D531D9B5B8D69DD6B6AB830EC4F229BB07491B5AEFB9
    private static readonly (float Weight, int[] Vertices)[] Far = ReadPackedFar();
    private static (float Weight, int[] Vertices)[] ReadPackedNear()
    {
        var d = Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values;
        var values = new (float Weight, int[] Vertices)[3];
        for (int i = 0; i < 1; i++)
            values[0 + i] = (-d.F32[1 + i * 1], new[] { d.I32[9 + i * 128], d.I32[10 + i * 128], d.I32[11 + i * 128], d.I32[12 + i * 128], d.I32[13 + i * 128], d.I32[14 + i * 128], d.I32[15 + i * 128], d.I32[16 + i * 128], d.I32[17 + i * 128], d.I32[18 + i * 128], d.I32[19 + i * 128], d.I32[20 + i * 128], d.I32[21 + i * 128], d.I32[22 + i * 128], d.I32[23 + i * 128], d.I32[24 + i * 128], d.I32[25 + i * 128], d.I32[26 + i * 128], d.I32[27 + i * 128], d.I32[28 + i * 128], d.I32[29 + i * 128], d.I32[30 + i * 128], d.I32[31 + i * 128], d.I32[32 + i * 128], d.I32[33 + i * 128], d.I32[34 + i * 128], d.I32[35 + i * 128], d.I32[36 + i * 128], d.I32[37 + i * 128], d.I32[38 + i * 128], d.I32[39 + i * 128], d.I32[40 + i * 128], d.I32[41 + i * 128], d.I32[42 + i * 128], d.I32[43 + i * 128], d.I32[44 + i * 128], d.I32[45 + i * 128], d.I32[46 + i * 128], d.I32[47 + i * 128], d.I32[48 + i * 128], d.I32[49 + i * 128], d.I32[50 + i * 128], d.I32[51 + i * 128], d.I32[52 + i * 128], d.I32[53 + i * 128], d.I32[54 + i * 128], d.I32[55 + i * 128], d.I32[56 + i * 128], d.I32[57 + i * 128], d.I32[58 + i * 128], d.I32[59 + i * 128], d.I32[60 + i * 128], d.I32[61 + i * 128], d.I32[62 + i * 128], d.I32[63 + i * 128], d.I32[64 + i * 128], d.I32[65 + i * 128], d.I32[66 + i * 128], d.I32[67 + i * 128], d.I32[68 + i * 128], d.I32[69 + i * 128], d.I32[70 + i * 128], d.I32[71 + i * 128], d.I32[72 + i * 128], d.I32[73 + i * 128], d.I32[74 + i * 128], d.I32[75 + i * 128], d.I32[76 + i * 128], d.I32[77 + i * 128], d.I32[78 + i * 128], d.I32[79 + i * 128], d.I32[80 + i * 128], d.I32[81 + i * 128], d.I32[82 + i * 128], d.I32[83 + i * 128], d.I32[84 + i * 128], d.I32[85 + i * 128], d.I32[86 + i * 128], d.I32[87 + i * 128], d.I32[88 + i * 128], d.I32[89 + i * 128], d.I32[90 + i * 128], d.I32[91 + i * 128], d.I32[92 + i * 128], d.I32[93 + i * 128], d.I32[94 + i * 128], d.I32[95 + i * 128], d.I32[96 + i * 128], d.I32[97 + i * 128], d.I32[98 + i * 128], d.I32[99 + i * 128], d.I32[100 + i * 128], d.I32[101 + i * 128], d.I32[102 + i * 128], d.I32[103 + i * 128], d.I32[104 + i * 128], d.I32[105 + i * 128], d.I32[106 + i * 128], d.I32[107 + i * 128], d.I32[108 + i * 128], d.I32[109 + i * 128], d.I32[110 + i * 128], d.I32[111 + i * 128], d.I32[112 + i * 128], d.I32[113 + i * 128], d.I32[114 + i * 128], d.I32[115 + i * 128], d.I32[116 + i * 128], d.I32[117 + i * 128], d.I32[118 + i * 128], d.I32[119 + i * 128], d.I32[120 + i * 128], d.I32[121 + i * 128], d.I32[122 + i * 128], d.I32[123 + i * 128], d.I32[124 + i * 128], d.I32[125 + i * 128], d.I32[126 + i * 128], d.I32[127 + i * 128], d.I32[128 + i * 128], d.I32[129 + i * 128], d.I32[130 + i * 128], d.I32[131 + i * 128], d.I32[132 + i * 128], d.I32[133 + i * 128], d.I32[134 + i * 128], d.I32[135 + i * 128], d.I32[136 + i * 128], });
        for (int i = 0; i < 1; i++)
            values[1 + i] = (d.F32[2 + i * 1], new[] { d.I32[137 + i * 32], d.I32[138 + i * 32], d.I32[139 + i * 32], d.I32[140 + i * 32], d.I32[141 + i * 32], d.I32[142 + i * 32], d.I32[143 + i * 32], d.I32[144 + i * 32], d.I32[145 + i * 32], d.I32[146 + i * 32], d.I32[147 + i * 32], d.I32[148 + i * 32], d.I32[149 + i * 32], d.I32[150 + i * 32], d.I32[151 + i * 32], d.I32[152 + i * 32], d.I32[153 + i * 32], d.I32[154 + i * 32], d.I32[155 + i * 32], d.I32[156 + i * 32], d.I32[157 + i * 32], d.I32[158 + i * 32], d.I32[159 + i * 32], d.I32[160 + i * 32], d.I32[161 + i * 32], d.I32[162 + i * 32], d.I32[163 + i * 32], d.I32[164 + i * 32], d.I32[165 + i * 32], d.I32[166 + i * 32], d.I32[167 + i * 32], d.I32[168 + i * 32], });
        for (int i = 0; i < 1; i++)
            values[2 + i] = (d.F32[3 + i * 1], new[] { d.I32[169 + i * 128], d.I32[170 + i * 128], d.I32[171 + i * 128], d.I32[172 + i * 128], d.I32[173 + i * 128], d.I32[174 + i * 128], d.I32[175 + i * 128], d.I32[176 + i * 128], d.I32[177 + i * 128], d.I32[178 + i * 128], d.I32[179 + i * 128], d.I32[180 + i * 128], d.I32[181 + i * 128], d.I32[182 + i * 128], d.I32[183 + i * 128], d.I32[184 + i * 128], d.I32[185 + i * 128], d.I32[186 + i * 128], d.I32[187 + i * 128], d.I32[188 + i * 128], d.I32[189 + i * 128], d.I32[190 + i * 128], d.I32[191 + i * 128], d.I32[192 + i * 128], d.I32[193 + i * 128], d.I32[194 + i * 128], d.I32[195 + i * 128], d.I32[196 + i * 128], d.I32[197 + i * 128], d.I32[198 + i * 128], d.I32[199 + i * 128], d.I32[200 + i * 128], d.I32[201 + i * 128], d.I32[202 + i * 128], d.I32[203 + i * 128], d.I32[204 + i * 128], d.I32[205 + i * 128], d.I32[206 + i * 128], d.I32[207 + i * 128], d.I32[208 + i * 128], d.I32[209 + i * 128], d.I32[210 + i * 128], d.I32[211 + i * 128], d.I32[212 + i * 128], d.I32[213 + i * 128], d.I32[214 + i * 128], d.I32[215 + i * 128], d.I32[216 + i * 128], d.I32[217 + i * 128], d.I32[218 + i * 128], d.I32[219 + i * 128], d.I32[220 + i * 128], d.I32[221 + i * 128], d.I32[222 + i * 128], d.I32[223 + i * 128], d.I32[224 + i * 128], d.I32[225 + i * 128], d.I32[226 + i * 128], d.I32[227 + i * 128], d.I32[228 + i * 128], d.I32[229 + i * 128], d.I32[230 + i * 128], d.I32[231 + i * 128], d.I32[232 + i * 128], d.I32[233 + i * 128], d.I32[234 + i * 128], d.I32[235 + i * 128], d.I32[236 + i * 128], d.I32[237 + i * 128], d.I32[238 + i * 128], d.I32[239 + i * 128], d.I32[240 + i * 128], d.I32[241 + i * 128], d.I32[242 + i * 128], d.I32[243 + i * 128], d.I32[244 + i * 128], d.I32[245 + i * 128], d.I32[246 + i * 128], d.I32[247 + i * 128], d.I32[248 + i * 128], d.I32[249 + i * 128], d.I32[250 + i * 128], d.I32[251 + i * 128], d.I32[252 + i * 128], d.I32[253 + i * 128], d.I32[254 + i * 128], d.I32[255 + i * 128], d.I32[256 + i * 128], d.I32[257 + i * 128], d.I32[258 + i * 128], d.I32[259 + i * 128], d.I32[260 + i * 128], d.I32[261 + i * 128], d.I32[262 + i * 128], d.I32[263 + i * 128], d.I32[264 + i * 128], d.I32[265 + i * 128], d.I32[266 + i * 128], d.I32[267 + i * 128], d.I32[268 + i * 128], d.I32[269 + i * 128], d.I32[270 + i * 128], d.I32[271 + i * 128], d.I32[272 + i * 128], d.I32[273 + i * 128], d.I32[274 + i * 128], d.I32[275 + i * 128], d.I32[276 + i * 128], d.I32[277 + i * 128], d.I32[278 + i * 128], d.I32[279 + i * 128], d.I32[280 + i * 128], d.I32[281 + i * 128], d.I32[282 + i * 128], d.I32[283 + i * 128], d.I32[284 + i * 128], d.I32[285 + i * 128], d.I32[286 + i * 128], d.I32[287 + i * 128], d.I32[288 + i * 128], d.I32[289 + i * 128], d.I32[290 + i * 128], d.I32[291 + i * 128], d.I32[292 + i * 128], d.I32[293 + i * 128], d.I32[294 + i * 128], d.I32[295 + i * 128], d.I32[296 + i * 128], });
        return values;
    }

    private static (float Weight, int[] Vertices)[] ReadPackedMiddle()
    {
        var d = Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values;
        var values = new (float Weight, int[] Vertices)[3];
        for (int i = 0; i < 1; i++)
            values[0 + i] = (-d.F32[4 + i * 1], new[] { d.I32[297 + i * 112], d.I32[298 + i * 112], d.I32[299 + i * 112], d.I32[300 + i * 112], d.I32[301 + i * 112], d.I32[302 + i * 112], d.I32[303 + i * 112], d.I32[304 + i * 112], d.I32[305 + i * 112], d.I32[306 + i * 112], d.I32[307 + i * 112], d.I32[308 + i * 112], d.I32[309 + i * 112], d.I32[310 + i * 112], d.I32[311 + i * 112], d.I32[312 + i * 112], d.I32[313 + i * 112], d.I32[314 + i * 112], d.I32[315 + i * 112], d.I32[316 + i * 112], d.I32[317 + i * 112], d.I32[318 + i * 112], d.I32[319 + i * 112], d.I32[320 + i * 112], d.I32[321 + i * 112], d.I32[322 + i * 112], d.I32[323 + i * 112], d.I32[324 + i * 112], d.I32[325 + i * 112], d.I32[326 + i * 112], d.I32[327 + i * 112], d.I32[328 + i * 112], d.I32[329 + i * 112], d.I32[330 + i * 112], d.I32[331 + i * 112], d.I32[332 + i * 112], d.I32[333 + i * 112], d.I32[334 + i * 112], d.I32[335 + i * 112], d.I32[336 + i * 112], d.I32[337 + i * 112], d.I32[338 + i * 112], d.I32[339 + i * 112], d.I32[340 + i * 112], d.I32[341 + i * 112], d.I32[342 + i * 112], d.I32[343 + i * 112], d.I32[344 + i * 112], d.I32[345 + i * 112], d.I32[346 + i * 112], d.I32[347 + i * 112], d.I32[348 + i * 112], d.I32[349 + i * 112], d.I32[350 + i * 112], d.I32[351 + i * 112], d.I32[352 + i * 112], d.I32[353 + i * 112], d.I32[354 + i * 112], d.I32[355 + i * 112], d.I32[356 + i * 112], d.I32[357 + i * 112], d.I32[358 + i * 112], d.I32[359 + i * 112], d.I32[360 + i * 112], d.I32[361 + i * 112], d.I32[362 + i * 112], d.I32[363 + i * 112], d.I32[364 + i * 112], d.I32[365 + i * 112], d.I32[366 + i * 112], d.I32[367 + i * 112], d.I32[368 + i * 112], d.I32[369 + i * 112], d.I32[370 + i * 112], d.I32[371 + i * 112], d.I32[372 + i * 112], d.I32[373 + i * 112], d.I32[374 + i * 112], d.I32[375 + i * 112], d.I32[376 + i * 112], d.I32[377 + i * 112], d.I32[378 + i * 112], d.I32[379 + i * 112], d.I32[380 + i * 112], d.I32[381 + i * 112], d.I32[382 + i * 112], d.I32[383 + i * 112], d.I32[384 + i * 112], d.I32[385 + i * 112], d.I32[386 + i * 112], d.I32[387 + i * 112], d.I32[388 + i * 112], d.I32[389 + i * 112], d.I32[390 + i * 112], d.I32[391 + i * 112], d.I32[392 + i * 112], d.I32[393 + i * 112], d.I32[394 + i * 112], d.I32[395 + i * 112], d.I32[396 + i * 112], d.I32[397 + i * 112], d.I32[398 + i * 112], d.I32[399 + i * 112], d.I32[400 + i * 112], d.I32[401 + i * 112], d.I32[402 + i * 112], d.I32[403 + i * 112], d.I32[404 + i * 112], d.I32[405 + i * 112], d.I32[406 + i * 112], d.I32[407 + i * 112], d.I32[408 + i * 112], });
        for (int i = 0; i < 1; i++)
            values[1 + i] = (d.F32[5 + i * 1], new[] { d.I32[409 + i * 32], d.I32[410 + i * 32], d.I32[411 + i * 32], d.I32[412 + i * 32], d.I32[413 + i * 32], d.I32[414 + i * 32], d.I32[415 + i * 32], d.I32[416 + i * 32], d.I32[417 + i * 32], d.I32[418 + i * 32], d.I32[419 + i * 32], d.I32[420 + i * 32], d.I32[421 + i * 32], d.I32[422 + i * 32], d.I32[423 + i * 32], d.I32[424 + i * 32], d.I32[425 + i * 32], d.I32[426 + i * 32], d.I32[427 + i * 32], d.I32[428 + i * 32], d.I32[429 + i * 32], d.I32[430 + i * 32], d.I32[431 + i * 32], d.I32[432 + i * 32], d.I32[433 + i * 32], d.I32[434 + i * 32], d.I32[435 + i * 32], d.I32[436 + i * 32], d.I32[437 + i * 32], d.I32[438 + i * 32], d.I32[439 + i * 32], d.I32[440 + i * 32], });
        for (int i = 0; i < 1; i++)
            values[2 + i] = (d.F32[6 + i * 1], new[] { d.I32[441 + i * 115], d.I32[442 + i * 115], d.I32[443 + i * 115], d.I32[444 + i * 115], d.I32[445 + i * 115], d.I32[446 + i * 115], d.I32[447 + i * 115], d.I32[448 + i * 115], d.I32[449 + i * 115], d.I32[450 + i * 115], d.I32[451 + i * 115], d.I32[452 + i * 115], d.I32[453 + i * 115], d.I32[454 + i * 115], d.I32[455 + i * 115], d.I32[456 + i * 115], d.I32[457 + i * 115], d.I32[458 + i * 115], d.I32[459 + i * 115], d.I32[460 + i * 115], d.I32[461 + i * 115], d.I32[462 + i * 115], d.I32[463 + i * 115], d.I32[464 + i * 115], d.I32[465 + i * 115], d.I32[466 + i * 115], d.I32[467 + i * 115], d.I32[468 + i * 115], d.I32[469 + i * 115], d.I32[470 + i * 115], d.I32[471 + i * 115], d.I32[472 + i * 115], d.I32[473 + i * 115], d.I32[474 + i * 115], d.I32[475 + i * 115], d.I32[476 + i * 115], d.I32[477 + i * 115], d.I32[478 + i * 115], d.I32[479 + i * 115], d.I32[480 + i * 115], d.I32[481 + i * 115], d.I32[482 + i * 115], d.I32[483 + i * 115], d.I32[484 + i * 115], d.I32[485 + i * 115], d.I32[486 + i * 115], d.I32[487 + i * 115], d.I32[488 + i * 115], d.I32[489 + i * 115], d.I32[490 + i * 115], d.I32[491 + i * 115], d.I32[492 + i * 115], d.I32[493 + i * 115], d.I32[494 + i * 115], d.I32[495 + i * 115], d.I32[496 + i * 115], d.I32[497 + i * 115], d.I32[498 + i * 115], d.I32[499 + i * 115], d.I32[500 + i * 115], d.I32[501 + i * 115], d.I32[502 + i * 115], d.I32[503 + i * 115], d.I32[504 + i * 115], d.I32[505 + i * 115], d.I32[506 + i * 115], d.I32[507 + i * 115], d.I32[508 + i * 115], d.I32[509 + i * 115], d.I32[510 + i * 115], d.I32[511 + i * 115], d.I32[512 + i * 115], d.I32[513 + i * 115], d.I32[514 + i * 115], d.I32[515 + i * 115], d.I32[516 + i * 115], d.I32[517 + i * 115], d.I32[518 + i * 115], d.I32[519 + i * 115], d.I32[520 + i * 115], d.I32[521 + i * 115], d.I32[522 + i * 115], d.I32[523 + i * 115], d.I32[524 + i * 115], d.I32[525 + i * 115], d.I32[526 + i * 115], d.I32[527 + i * 115], d.I32[528 + i * 115], d.I32[529 + i * 115], d.I32[530 + i * 115], d.I32[531 + i * 115], d.I32[532 + i * 115], d.I32[533 + i * 115], d.I32[534 + i * 115], d.I32[535 + i * 115], d.I32[536 + i * 115], d.I32[537 + i * 115], d.I32[538 + i * 115], d.I32[539 + i * 115], d.I32[540 + i * 115], d.I32[541 + i * 115], d.I32[542 + i * 115], d.I32[543 + i * 115], d.I32[544 + i * 115], d.I32[545 + i * 115], d.I32[546 + i * 115], d.I32[547 + i * 115], d.I32[548 + i * 115], d.I32[549 + i * 115], d.I32[550 + i * 115], d.I32[551 + i * 115], d.I32[552 + i * 115], d.I32[553 + i * 115], d.I32[554 + i * 115], d.I32[555 + i * 115], });
        return values;
    }

    private static (float Weight, int[] Vertices)[] ReadPackedFar()
    {
        var d = Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs.Values;
        var values = new (float Weight, int[] Vertices)[9];
        for (int i = 0; i < 1; i++)
            values[0 + i] = (-d.F32[7 + i * 1], new[] { d.I32[556 + i * 35], d.I32[557 + i * 35], d.I32[558 + i * 35], d.I32[559 + i * 35], d.I32[560 + i * 35], d.I32[561 + i * 35], d.I32[562 + i * 35], d.I32[563 + i * 35], d.I32[564 + i * 35], d.I32[565 + i * 35], d.I32[566 + i * 35], d.I32[567 + i * 35], d.I32[568 + i * 35], d.I32[569 + i * 35], d.I32[570 + i * 35], d.I32[571 + i * 35], d.I32[572 + i * 35], d.I32[573 + i * 35], d.I32[574 + i * 35], d.I32[575 + i * 35], d.I32[576 + i * 35], d.I32[577 + i * 35], d.I32[578 + i * 35], d.I32[579 + i * 35], d.I32[580 + i * 35], d.I32[581 + i * 35], d.I32[582 + i * 35], d.I32[583 + i * 35], d.I32[584 + i * 35], d.I32[585 + i * 35], d.I32[586 + i * 35], d.I32[587 + i * 35], d.I32[588 + i * 35], d.I32[589 + i * 35], d.I32[590 + i * 35], });
        for (int i = 0; i < 2; i++)
            values[1 + i] = (-d.F32[8 + i * 1], new[] { d.I32[591 + i * 2], d.I32[592 + i * 2], });
        for (int i = 0; i < 1; i++)
            values[3 + i] = (-d.F32[10 + i * 1], new[] { d.I32[595 + i * 4], d.I32[596 + i * 4], d.I32[597 + i * 4], d.I32[598 + i * 4], });
        for (int i = 0; i < 1; i++)
            values[4 + i] = (-d.F32[11 + i * 1], new[] { d.I32[599 + i * 2], d.I32[600 + i * 2], });
        for (int i = 0; i < 1; i++)
            values[5 + i] = (d.F32[12 + i * 1], new[] { d.I32[601 + i * 26], d.I32[602 + i * 26], d.I32[603 + i * 26], d.I32[604 + i * 26], d.I32[605 + i * 26], d.I32[606 + i * 26], d.I32[607 + i * 26], d.I32[608 + i * 26], d.I32[609 + i * 26], d.I32[610 + i * 26], d.I32[611 + i * 26], d.I32[612 + i * 26], d.I32[613 + i * 26], d.I32[614 + i * 26], d.I32[615 + i * 26], d.I32[616 + i * 26], d.I32[617 + i * 26], d.I32[618 + i * 26], d.I32[619 + i * 26], d.I32[620 + i * 26], d.I32[621 + i * 26], d.I32[622 + i * 26], d.I32[623 + i * 26], d.I32[624 + i * 26], d.I32[625 + i * 26], d.I32[626 + i * 26], });
        for (int i = 0; i < 2; i++)
            values[6 + i] = (d.F32[13 + i * 1], new[] { d.I32[627 + i * 2], d.I32[628 + i * 2], });
        for (int i = 0; i < 1; i++)
            values[8 + i] = (d.F32[15 + i * 1], new[] { d.I32[631 + i * 56], d.I32[632 + i * 56], d.I32[633 + i * 56], d.I32[634 + i * 56], d.I32[635 + i * 56], d.I32[636 + i * 56], d.I32[637 + i * 56], d.I32[638 + i * 56], d.I32[639 + i * 56], d.I32[640 + i * 56], d.I32[641 + i * 56], d.I32[642 + i * 56], d.I32[643 + i * 56], d.I32[644 + i * 56], d.I32[645 + i * 56], d.I32[646 + i * 56], d.I32[647 + i * 56], d.I32[648 + i * 56], d.I32[649 + i * 56], d.I32[650 + i * 56], d.I32[651 + i * 56], d.I32[652 + i * 56], d.I32[653 + i * 56], d.I32[654 + i * 56], d.I32[655 + i * 56], d.I32[656 + i * 56], d.I32[657 + i * 56], d.I32[658 + i * 56], d.I32[659 + i * 56], d.I32[660 + i * 56], d.I32[661 + i * 56], d.I32[662 + i * 56], d.I32[663 + i * 56], d.I32[664 + i * 56], d.I32[665 + i * 56], d.I32[666 + i * 56], d.I32[667 + i * 56], d.I32[668 + i * 56], d.I32[669 + i * 56], d.I32[670 + i * 56], d.I32[671 + i * 56], d.I32[672 + i * 56], d.I32[673 + i * 56], d.I32[674 + i * 56], d.I32[675 + i * 56], d.I32[676 + i * 56], d.I32[677 + i * 56], d.I32[678 + i * 56], d.I32[679 + i * 56], d.I32[680 + i * 56], d.I32[681 + i * 56], d.I32[682 + i * 56], d.I32[683 + i * 56], d.I32[684 + i * 56], d.I32[685 + i * 56], d.I32[686 + i * 56], });
        return values;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01ColumnWideningData_g_cs
internal static class Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs
{
    static Numeric_Types_Draw01_Draw01ColumnWideningData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01ColumnWideningData_g_cs");
}
