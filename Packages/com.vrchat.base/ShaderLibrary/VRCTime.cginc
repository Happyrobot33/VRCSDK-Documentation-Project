#ifndef VRCTIME_CGINC
#define VRCTIME_CGINC

uint _VRChatTimeUTCUnixSeconds;
uint _VRChatTimeNetworkMs;
uint _VRChatTimeEncoded1;
uint _VRChatTimeEncoded2;

uint VRC_GetUTCUnixTimeInSeconds() {
    return _VRChatTimeUTCUnixSeconds;
}

uint VRC_GetNetworkTimeInMilliseconds() {
    return _VRChatTimeNetworkMs;
}

void VRC_GetUTCTime(out uint hours, out uint minutes, out uint seconds, out uint milliseconds) {
    uint t1 = asuint(_VRChatTimeEncoded1);
    uint t2 = asuint(_VRChatTimeEncoded2); // t2 access should be optimized away if `milliseconds` is unused
    hours = t1 & 0x1F;
    minutes = (t1 >> 5) & 0x3F;
    seconds = (t1 >> 11) & 0x3F;
    milliseconds = t2 & 0x3FF;
}

void VRC_GetLocalTime(out uint hours, out uint minutes, out uint seconds, out uint milliseconds) {
    uint t1 = asuint(_VRChatTimeEncoded1);
    uint t2 = asuint(_VRChatTimeEncoded2);
    hours = (t1 >> 17) & 0x1F;
    minutes = (t1 >> 22) & 0x3F;
    seconds = (t1 >> 11) & 0x3F; // shared with UTC
    milliseconds = t2 & 0x3FF; // shared with UTC
}

int VRC_GetTimezoneOffsetSeconds() {
    uint t2 = asuint(_VRChatTimeEncoded2);
    int offset = (t2 >> 11) & 0xFFFF;
    if (t2 & (1 << 10)) // sign bit
        offset *= -1;
    return offset;
}

#endif // VRCTIME_CGINC