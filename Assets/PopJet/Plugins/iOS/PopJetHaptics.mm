// Rung (haptics) cho iOS, gọi từ PopJet.Haptics (DllImport "__Internal").
// Chỉ build cho iOS: PopJetSceneBuilder đặt Plugin Import Settings = iOS.
#import <UIKit/UIKit.h>

extern "C" void PopJetHaptics_Impact(int style)
{
    if (@available(iOS 10.0, *)) {
        UIImpactFeedbackStyle s = UIImpactFeedbackStyleLight;
        if (style == 1) s = UIImpactFeedbackStyleMedium;
        else if (style == 2) s = UIImpactFeedbackStyleHeavy;
        UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
        [g prepare];
        [g impactOccurred];
#if !__has_feature(objc_arc)
        [g release];
#endif
    }
}

extern "C" void PopJetHaptics_Selection(void)
{
    if (@available(iOS 10.0, *)) {
        UISelectionFeedbackGenerator *g = [[UISelectionFeedbackGenerator alloc] init];
        [g prepare];
        [g selectionChanged];
#if !__has_feature(objc_arc)
        [g release];
#endif
    }
}

extern "C" void PopJetHaptics_Success(void)
{
    if (@available(iOS 10.0, *)) {
        UINotificationFeedbackGenerator *g = [[UINotificationFeedbackGenerator alloc] init];
        [g prepare];
        [g notificationOccurred:UINotificationFeedbackTypeSuccess];
#if !__has_feature(objc_arc)
        [g release];
#endif
    }
}
