export const EORZEA_MULTIPLIER = 3600 / 175;

export const getEorzeaTime = (date: Date): Date => {
    const epoch = date.getTime();
    const eorzeaEpoch = epoch * EORZEA_MULTIPLIER;
    return new Date(eorzeaEpoch);
};

export const formatTime = (date: Date): string => {
    return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false });
};
