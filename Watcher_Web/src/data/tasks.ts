import type { UtilityTask } from '../types';

export const TASKS: UtilityTask[] = [
    // 1. 隨機任務 (Duty Roulette)
    {
        id: 'roulette_level50_60_70_80',
        name: '拾級迷宮',
        frequency: 'daily',
        category: 'duty_roulette',
        description: 'Level 50/60/70/80',
        resetTime: 23,
    },
    {
        id: 'roulette_leveling',
        name: '練級',
        frequency: 'daily',
        category: 'duty_roulette',
        description: 'Leveling',
        resetTime: 23,
    },
    {
        id: 'roulette_trials',
        name: '討伐殲滅戰',
        frequency: 'daily',
        category: 'duty_roulette',
        description: 'Trials',
        resetTime: 23,
    },
    {
        id: 'roulette_msq',
        name: '主線任務',
        frequency: 'daily',
        category: 'duty_roulette',
        description: 'Main Scenario',
        resetTime: 23,
    },
    {
        id: 'roulette_alliance',
        name: '團隊任務',
        frequency: 'daily',
        category: 'duty_roulette',
        description: 'Alliance Raids',
        resetTime: 23,
    },
    {
        id: 'roulette_normal',
        name: '大型任務',
        frequency: 'daily',
        category: 'duty_roulette',
        description: 'Normal Raids',
        resetTime: 23,
    },

    // 2. 每日挑戰 (Daily Challenge)
    {
        id: 'roulette_frontline',
        name: '紛爭前線',
        frequency: 'daily',
        category: 'daily_challenge',
        description: 'Frontline',
        resetTime: 23,
    },

    // 3. 任務情報 (Mission Info) - Original Items
    {
        id: 'grand_company_supply',
        name: '籌備任務',
        frequency: 'daily',
        category: 'mission_info',
        description: 'Grand Company Supply',
        resetTime: 23,
    },
    {
        id: 'tribal_quests',
        name: '友好部族',
        frequency: 'daily',
        category: 'mission_info',
        description: 'Tribal Quests',
        resetTime: 23,
    },
    {
        id: 'custom_deliveries',
        name: '老主顧',
        frequency: 'weekly',
        category: 'mission_info',
        description: 'Custom Deliveries',
        resetDay: 2,
        resetTime: 16,
    },
    {
        id: 'fashion_report',
        name: '時尚品鑑',
        frequency: 'weekly',
        category: 'mission_info',
        description: 'Fashion Report',
        resetDay: 2,
        resetTime: 16,
    },
];
