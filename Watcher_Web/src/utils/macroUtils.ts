// export const JOB_LIST = [
//     { name: '刻木匠 (Carpenter)', set: 1 },
//     { name: '鍛鐵匠 (Blacksmith)', set: 2 },
//     { name: '鑄甲匠 (Armorer)', set: 3 },
//     { name: '雕金匠 (Goldsmith)', set: 4 },
//     { name: '製革匠 (Leatherworker)', set: 5 },
//     { name: '裁衣匠 (Weaver)', set: 6 },
//     { name: '鍊金術士 (Alchemist)', set: 7 },
//     { name: '烹調師 (Culinarian)', set: 8 },
//     { name: '採掘師 (Miner)', set: 9 },
//     { name: '園藝師 (Botanist)', set: 10 },
//     { name: '漁師 (Fisher)', set: 11 },
// ];

export const generateJobCycleMacro = (): string => {
    // Hardcoded macro for now, as we don't know user's specific gearset IDs but names are generally safer
    // provided they have sets named or use abbreviations if supported.
    // Using standard 3-letter abbreviations is the most robust method for /gearset change if sets are named so,
    // OR if the game supports job abbreviation for change (Verified: /gearset change "JOB" works if set exists and is named JOB, or sometimes simply by ID.
    // Safest default for general users is manual setup or just copy paste.

    return `/echo === 開始掃描職業 ===
/gearset change 木工師 <wait.3>
/gearset change 鍛造師 <wait.3>
/gearset change 甲冑師 <wait.3>
/gearset change 金工師 <wait.3>
/gearset change 皮革師 <wait.3>
/gearset change 裁縫師 <wait.3>
/gearset change 鍊金術士 <wait.3>
/gearset change 烹調師 <wait.3>
/gearset change 採掘師 <wait.3>
/gearset change 園藝師 <wait.3>
/gearset change 漁師 <wait.3>
/echo === 掃描結束 ===`;
};
