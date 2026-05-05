namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class EndOfTheRoadMapGeometry
{
    public static IReadOnlyDictionary<string, MapSectionGeometry> CreateSections() =>
        new Dictionary<string, MapSectionGeometry>(StringComparer.OrdinalIgnoreCase)
        {
            ["end-of-the-road/umbrella-research-facility"] = new(
                725,
                694,
                [
                    new(
                        6,
                        "laser-emission-room",
                        MapRoomShape.Polygon,
                        "55,93,79,93,79,305,13,305,13,262,40,235,40,108"
                    ),
                    new(
                        5,
                        "central-passage-3",
                        MapRoomShape.Polygon,
                        "84,80,156,8,224,8,224,13,230,13,230,8,291,8,291,45,230,45,230,35,224,35,224,45,172,45,119,98,119,155,84,155"
                    ),
                    new(
                        3,
                        "central-passage-2",
                        MapRoomShape.Polygon,
                        "84,160,119,160,119,226,111,226,111,231,119,231,119,292,171,344,220,344,220,382,155,382,84,311,84,231,92,231,92,226,84,226"
                    ),
                    new(
                        14,
                        "central-passage-4",
                        MapRoomShape.Polygon,
                        "296,8,362,8,432,78,432,158,428,158,428,163,432,163,432,230,398,230,398,163,407,163,407,158,398,158,398,98,345,45,296,45"
                    ),
                    new(
                        10,
                        "stairwell",
                        MapRoomShape.Polygon,
                        "212,65,319,65,319,85,316,88,316,89,344,117,345,117,350,112,393,112,393,139,332,139,332,130,334,128,334,127,306,99,305,99,301,103,212,103"
                    ),
                    new(9, "observation-mezzanine", MapRoomShape.Rectangle, "212,108,294,190"),
                    new(
                        8,
                        "experimentation-chamber",
                        MapRoomShape.Polygon,
                        "300,136,328,136,328,282,337,282,337,340,310,340,310,277,190,277,190,193,300,193"
                    ),
                    new(
                        2,
                        "central-passage-1",
                        MapRoomShape.Polygon,
                        "225,345,288,345,288,355,293,355,293,345,345,345,398,292,398,235,433,235,433,311,362,382,293,382,293,378,288,378,288,382,225,382"
                    ),
                    new(
                        4,
                        "west-passage",
                        MapRoomShape.Polygon,
                        "13,310,76,310,149,383,149,462,143,462,143,467,149,467,149,532,113,532,113,467,122,467,122,462,113,462,113,401,61,349,13,349"
                    ),
                    new(7, "examination-room", MapRoomShape.Polygon, "154,387,238,387,238,433,190,433,190,415,154,415"),
                    new(
                        1,
                        "waiting-room",
                        MapRoomShape.Polygon,
                        "243,387,273,387,273,495,269,495,269,500,295,500,295,561,294,562,274,567,237,567,217,562,216,561,216,500,247,500,247,495,243,495"
                    ),
                    new(13, "mainframe", MapRoomShape.Polygon, "438,235,479,235,505,261,505,306,438,306"),
                    new(11, "reference-room", MapRoomShape.Polygon, "287,387,363,387,363,415,328,415,328,433,287,433"),
                    new(
                        12,
                        "east-passage-1",
                        MapRoomShape.Polygon,
                        "440,311,507,311,507,316,514,316,514,311,575,311,575,349,514,349,514,339,507,339,507,349,455,349,403,401,403,458,368,458,368,383"
                    ),
                    new(
                        15,
                        "special-research-room",
                        MapRoomShape.Polygon,
                        "457,354,504,354,504,406,457,453,457,495,408,495,408,403"
                    ),
                    new(
                        16,
                        "east-passage-2",
                        MapRoomShape.Polygon,
                        "368,463,403,463,403,529,394,529,394,534,403,534,403,595,455,647,504,647,504,685,440,685,368,613,368,534,372,534,372,529,368,529"
                    ),
                    new(20, "passage-in-front-of-elevator", MapRoomShape.Rectangle, "580,311,615,456"),
                    new(
                        17,
                        "east-passage-3",
                        MapRoomShape.Polygon,
                        "509,647,572,647,572,656,576,656,576,647,631,647,682,596,682,540,717,540,717,613,645,685,576,685,576,680,572,680,572,685,509,685"
                    ),
                    new(18, "nursery", MapRoomShape.Polygon, "544,592,578,592,630,540,677,540,677,594,629,642,544,642"),
                    new(19, "east-exit", MapRoomShape.Rectangle, "682,387,716,534"),
                ],
                CreateLinks(
                    ("waiting-room", "central-passage-1"),
                    ("waiting-room", "examination-room"),
                    ("central-passage-1", "central-passage-2"),
                    ("central-passage-1", "central-passage-4"),
                    ("central-passage-1", "east-passage-1"),
                    ("central-passage-1", "experimentation-chamber"),
                    ("central-passage-1", "mainframe"),
                    ("central-passage-2", "central-passage-3"),
                    ("central-passage-2", "examination-room"),
                    ("central-passage-2", "laser-emission-room"),
                    ("central-passage-2", "west-passage"),
                    ("central-passage-4", "stairwell"),
                    ("west-passage", "central-passage-1"),
                    ("experimentation-chamber", "observation-mezzanine"),
                    ("observation-mezzanine", "stairwell"),
                    ("east-passage-1", "east-passage-2"),
                    ("east-passage-1", "passage-in-front-of-elevator"),
                    ("east-passage-1", "reference-room"),
                    ("east-passage-1", "special-research-room"),
                    ("east-passage-2", "east-passage-3"),
                    ("east-passage-3", "east-exit"),
                    ("east-passage-3", "nursery")
                ),
                detailAssetBucket: "reoutbreak2_umbrellaresearchfacility",
                details: CreateDetails(
                    Door(79, 97, 5, 10),
                    Door(221, 103, 10, 5),
                    Door(393, 120, 5, 10),
                    Door(295, 137, 5, 10),
                    Door(92, 155, 10, 5),
                    Door(79, 182, 5, 10),
                    Door(93, 227, 17, 3),
                    Door(407, 230, 10, 5),
                    Door(433, 271, 5, 10),
                    Door(58, 305, 10, 5),
                    Door(575, 320, 5, 10),
                    Door(105, 335, 11, 11),
                    Door(313, 340, 10, 5),
                    Door(392, 345, 11, 11),
                    Door(469, 349, 10, 5),
                    Door(220, 355, 5, 10),
                    Door(167, 382, 10, 5),
                    Door(247, 382, 10, 5),
                    Door(363, 400, 5, 10),
                    Door(238, 415, 5, 10),
                    Door(376, 458, 10, 5),
                    Door(592, 457, 10, 5),
                    Door(123, 463, 19, 3),
                    Door(690, 535, 10, 5),
                    Door(598, 642, 10, 5),
                    Door(504, 657, 5, 10),
                    Ladder(320, 66, 18, 46, MapSectionDetailOrientation.Vertical),
                    Stairs(301, 139, 18, 52, MapSectionDetailOrientation.Vertical)
                )
            ),
            ["end-of-the-road/water-treatment-plant-b1f"] = new(
                504,
                747,
                [
                    new(
                        37,
                        "maintenance-passage-2",
                        MapRoomShape.Polygon,
                        "16,150,16,137,15,137,15,133,19,129,20,129,24,125,24,124,25,122,32,118,33,118,38,123,38,129,35,135,35,145,64,174,143,174,143,192,54,192,19,157,19,153,21,151,23,151,23,150"
                    ),
                    new(38, "break-room", MapRoomShape.Rectangle, "76,197,121,243"),
                    new(
                        28,
                        "maintenance-passage-1",
                        MapRoomShape.Polygon,
                        "148,174,186,174,186,150,205,150,205,193,237,225,246,225,251,230,251,244,236,259,235,259,224,248,223,248,199,272,198,272,185,259,185,258,211,232,211,231,172,192,148,192"
                    ),
                    new(
                        29,
                        "floodgate-control-room",
                        MapRoomShape.Polygon,
                        "258,245,292,279,292,280,279,293,279,294,288,303,288,340,269,359,268,359,243,334,242,334,226,350,225,350,179,304,179,303,222,260,223,260,232,269,233,269,257,245"
                    ),
                    new(
                        39,
                        "north-waterway",
                        MapRoomShape.Polygon,
                        "294,0,345,0,345,37,407,37,407,51,354,51,354,204,281,204,281,47,303,47,303,38,294,38"
                    ),
                    new(
                        30,
                        "underground-waterworks",
                        MapRoomShape.Polygon,
                        "302,227,354,227,354,332,370,332,370,354,335,354,335,395,363,423,363,424,308,479,307,479,293,465,293,464,301,456,301,455,288,442,286,442,286,467,268,467,268,415,250,415,250,377,266,377,302,341"
                    ),
                    new(
                        31,
                        "maintenance-room",
                        MapRoomShape.Polygon,
                        "375,332,412,332,429,315,430,315,455,340,455,397,375,397"
                    ),
                    new(
                        32,
                        "drainage-area",
                        MapRoomShape.Polygon,
                        "396,403,402,403,402,402,416,402,416,403,420,403,420,442,504,526,504,570,464,610,428,610,383,565,383,544,368,529,367,529,352,544,337,529,352,514,352,513,347,508,346,508,343,511,342,511,333,502,333,491,396,428"
                    ),
                    new(
                        48,
                        "old-waterway",
                        MapRoomShape.Polygon,
                        "337,529,352,544,325,571,330,584,330,587,325,590,307,593,304,593,304,594,303,594,297,600,293,602,276,615,276,671,274,675,258,691,258,747,185,747,185,692,152,692,152,643,170,643,170,672,240,672,255,657,255,656,258,653,258,624,255,624,239,632,226,632,216,627,206,627,184,638,151,638,122,667,122,675,120,681,120,685,111,694,106,694,101,691,101,663,135,629,138,623,147,618,168,618,182,616,200,603,206,600,217,589,222,581,231,572,237,552,237,548,219,536,208,527,189,519,174,519,171,525,164,532,152,538,135,545,116,555,106,568,102,576,96,602,99,616,101,618,102,618,112,608,113,608,132,627,132,628,82,678,69,678,69,650,74,650,91,633,90,633,86,629,79,618,70,597,73,569,83,549,90,543,127,529,136,524,153,517,159,513,160,511,160,509,147,499,120,483,115,478,113,475,107,460,107,448,111,438,118,431,128,426,139,419,150,415,155,414,163,414,173,416,189,416,193,419,195,422,195,431,196,431,196,438,195,438,195,442,191,447,176,455,159,455,141,451,136,443,131,443,127,449,127,454,132,464,140,472,160,485,172,497,172,498,175,501,188,501,190,499,207,499,211,500,220,513,229,522,241,523,241,526,260,537,268,539,268,543,265,548,257,581,257,585,258,586,279,584,285,580,286,580,295,571,296,571,305,562,305,561"
                    ),
                ],
                CreateLinks(
                    ("maintenance-passage-2", "break-room"),
                    ("maintenance-passage-2", "maintenance-passage-1"),
                    ("maintenance-passage-1", "emergency-materials-storage"),
                    ("maintenance-passage-1", "floodgate-control-room"),
                    ("floodgate-control-room", "underground-waterworks"),
                    ("north-waterway", "main-street-south"),
                    ("north-waterway", "underground-waterworks"),
                    ("underground-waterworks", "drainage-area"),
                    ("underground-waterworks", "maintenance-room"),
                    ("underground-waterworks", "old-waterway"),
                    ("underground-waterworks", "passage-in-front-of-elevator"),
                    ("maintenance-room", "drainage-area"),
                    ("drainage-area", "old-waterway")
                ),
                detailAssetBucket: "reoutbreak2_watertreatmentplantb1",
                details: CreateDetails(
                    Door(143, 178, 5, 10),
                    Door(104, 192, 10, 5),
                    Door(339, 204, 10, 5),
                    Door(339, 222, 10, 5),
                    Door(238, 249, 11, 11),
                    Door(288, 320, 5, 10),
                    Door(297, 320, 5, 10),
                    Door(370, 336, 5, 10),
                    Door(404, 397, 10, 5),
                    Door(317, 462, 11, 11),
                    Door(272, 467, 10, 5),
                    Door(333, 477, 11, 11),
                    Door(157, 638, 10, 5),
                    Stairs(24, 118, 29, 25, MapSectionDetailOrientation.DiagonalDown),
                    Ladder(191, 207, 12, 37, MapSectionDetailOrientation.Vertical),
                    Ladder(317, 52, 13, 114, MapSectionDetailOrientation.Vertical),
                    Stairs(421, 47, 22, 8, MapSectionDetailOrientation.Horizontal),
                    Stairs(336, 473, 38, 36, MapSectionDetailOrientation.DiagonalDown)
                )
            ),
            ["end-of-the-road/water-treatment-plant-b2f"] = new(
                124,
                285,
                [
                    new(
                        45,
                        "emergency-materials-storage",
                        MapRoomShape.Polygon,
                        "0,56,39,56,39,0,54,0,54,56,65,56,65,47,79,47,79,56,124,56,124,141,71,141,71,113,0,113"
                    ),
                    new(46, "maintenance-passage-3", MapRoomShape.Rectangle, "18,113,34,284"),
                ],
                CreateLinks(
                    ("emergency-materials-storage", "maintenance-passage-1"),
                    ("emergency-materials-storage", "maintenance-passage-3"),
                    ("maintenance-passage-3", "old-waterway")
                ),
                detailAssetBucket: "reoutbreak2_watertreatmentplantb2",
                details: CreateDetails(Ladder(19, 113, 15, 152, MapSectionDetailOrientation.Vertical))
            ),
            ["end-of-the-road/urban-area-downtown"] = new(
                655,
                836,
                [
                    new(
                        59,
                        "under-the-highway-overpass",
                        MapRoomShape.Polygon,
                        "27,158,28,157,32,155,44,152,68,152,92,156,96,160,96,188,103,188,103,285,106,285,106,359,95,359,95,475,87,475,83,479,75,479,75,480,72,480,69,477,69,473,67,471,63,471,63,475,49,475,47,477,38,477,38,479,27,479"
                    ),
                    new(
                        61,
                        "office-building-1f",
                        MapRoomShape.Polygon,
                        "108,191,120,191,120,264,144,264,144,259,138,259,138,247,172,247,172,191,197,191,197,226,185,226,185,259,154,259,154,264,189,264,189,234,197,234,197,280,108,280"
                    ),
                    new(
                        58,
                        "construction-site",
                        MapRoomShape.Polygon,
                        "100,367,199,367,199,482,173,482,173,459,152,459,152,426,100,426"
                    ),
                    new(
                        53,
                        "in-front-of-apple-inn",
                        MapRoomShape.Polygon,
                        "96,487,152,487,152,464,168,464,168,487,264,487,264,472,305,472,305,357,317,353,334,349,339,349,339,467,326,467,326,488,359,488,359,523,358,527,355,527,355,531,367,555,367,560,318,560,318,615,302,615,302,560,198,560,198,573,173,598,172,598,154,580,154,579,166,567,166,560,100,560,90,545,90,539,117,539,117,527,101,524,96,524,96,520,94,520,92,518,92,514,90,512,90,502,97,502,99,500,99,498,96,495"
                    ),
                    new(
                        66,
                        "apple-inn-front-lobby",
                        MapRoomShape.Polygon,
                        "236,605,297,605,297,620,352,620,352,679,236,679"
                    ),
                    new(60, "office-building-warehouse", MapRoomShape.Rectangle, "202,231,302,302"),
                    new(
                        54,
                        "behind-the-residential-area",
                        MapRoomShape.Polygon,
                        "202,197,307,197,307,89,289,89,289,70,307,70,307,24,326,18,339,24,339,70,346,70,346,76,410,76,410,61,442,61,442,72,422,72,422,76,466,76,466,92,339,92,339,209,348,227,382,227,388,239,388,240,387,241,347,241,339,249,339,334,306,345,305,345,305,307,307,307,307,226,202,226"
                    ),
                    new(
                        55,
                        "footbridge",
                        MapRoomShape.Polygon,
                        "499,76,499,98,610,98,610,75,643,75,643,181,612,181,612,116,466,116,466,76"
                    ),
                    new(
                        56,
                        "main-street-north",
                        MapRoomShape.Polygon,
                        "555,120,612,120,612,132,609,132,609,479,594,479,594,482,596,484,596,497,597,497,597,508,571,508,565,496,561,490,554,483,553,483,542,472,500,472,500,240,542,240,542,297,544,304,550,310,554,310,560,304,562,297,562,210,560,203,554,197,550,197,544,203,542,210,542,240,479,240,479,218,491,218,500,191,500,141,510,136,518,135,527,129,534,129,536,127,547,127,549,125,555,125"
                    ),
                    new(
                        65,
                        "inside-the-helicopter",
                        MapRoomShape.Polygon,
                        "542,210,544,203,550,197,554,197,560,203,562,210,562,297,560,304,554,310,550,310,544,304,542,297"
                    ),
                    new(
                        57,
                        "main-street-south",
                        MapRoomShape.Polygon,
                        "586,513,599,513,599,529,618,567,618,574,612,581,622,623,622,627,620,627,620,635,613,671,634,671,634,702,621,702,616,697,608,693,605,692,569,683,548,680,500,680,500,650,486,650,486,643,500,643,500,554,501,553,507,550,507,548,511,548,529,539,531,539,541,551,547,552,579,544,580,544,583,541,586,541"
                    ),
                    new(
                        52,
                        "tunnel",
                        MapRoomShape.Polygon,
                        "326,467,469,467,469,836,452,836,452,587,455,587,455,488,326,488"
                    ),
                ],
                CreateLinks(
                    ("under-the-highway-overpass", "construction-site"),
                    ("under-the-highway-overpass", "office-building-1f"),
                    ("office-building-1f", "office-building-stairwell"),
                    ("office-building-1f", "office-building-warehouse"),
                    ("construction-site", "in-front-of-apple-inn"),
                    ("in-front-of-apple-inn", "apple-inn-front-lobby"),
                    ("in-front-of-apple-inn", "tunnel"),
                    ("behind-the-residential-area", "footbridge"),
                    ("behind-the-residential-area", "office-building-1f"),
                    ("behind-the-residential-area", "office-building-warehouse"),
                    ("footbridge", "main-street-north"),
                    ("main-street-north", "inside-the-helicopter"),
                    ("main-street-north", "main-street-south"),
                    ("main-street-south", "north-waterway")
                ),
                detailAssetBucket: "reoutbreak2_urbanareadowntown",
                details: CreateDetails(
                    Door(103, 212, 5, 10),
                    Door(272, 226, 10, 5),
                    Door(197, 238, 5, 10),
                    Door(144, 259, 10, 5),
                    Door(95, 413, 5, 10),
                    Door(156, 459, 10, 5),
                    Door(307, 615, 10, 5),
                    Ladder(177, 193, 16, 45, MapSectionDetailOrientation.Vertical),
                    Stairs(350, 76, 70, 13, MapSectionDetailOrientation.Horizontal),
                    Ladder(613, 121, 18, 42, MapSectionDetailOrientation.Vertical)
                )
            ),
            ["end-of-the-road/urban-area-highway"] = new(
                327,
                473,
                [
                    new(
                        63,
                        "elevated-highway",
                        MapRoomShape.Polygon,
                        "0,27,1,26,25,14,36,14,40,15,55,20,93,1,98,0,104,0,111,7,111,107,115,107,115,123,111,123,111,342,115,342,115,382,111,382,111,473,52,473,1,456,0,455"
                    ),
                    new(
                        64,
                        "rooftop",
                        MapRoomShape.Polygon,
                        "123,30,187,30,187,88,215,88,215,123,118,123,118,107,123,107"
                    ),
                    new(62, "office-building-stairwell", MapRoomShape.Rectangle, "190,30,214,82"),
                ],
                CreateLinks(
                    ("elevated-highway", "rooftop"),
                    ("rooftop", "office-building-stairwell"),
                    ("office-building-stairwell", "office-building-1f")
                ),
                detailAssetBucket: "reoutbreak2_urbanareahighway",
                details: CreateDetails(
                    Door(198, 83, 10, 5),
                    Ladder(191, 31, 22, 51, MapSectionDetailOrientation.Vertical)
                )
            ),
        };

    private static IReadOnlyList<MapSectionDetailGeometry> CreateDetails(params MapSectionDetailGeometry[] details) =>
        details;

    private static IReadOnlyList<MapRoomLink> CreateLinks(params (string sourceSlug, string targetSlug)[] pairs) =>
        pairs.Select(static pair => new MapRoomLink(pair.sourceSlug, pair.targetSlug)).ToArray();

    private static MapSectionDetailGeometry Door(int x, int y, int width, int height) =>
        new(MapSectionDetailKind.Door, x, y, width, height);

    private static MapSectionDetailGeometry Stairs(
        int x,
        int y,
        int width,
        int height,
        MapSectionDetailOrientation orientation
    ) => new(MapSectionDetailKind.Stairs, x, y, width, height, orientation);

    private static MapSectionDetailGeometry Ladder(
        int x,
        int y,
        int width,
        int height,
        MapSectionDetailOrientation orientation
    ) => new(MapSectionDetailKind.Ladder, x, y, width, height, orientation);
}
