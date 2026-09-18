using PDFMerger.Infrastructure;

namespace PDFMerger.Tests.Fixtures;

public class I18nFixture
{
    public I18nFixture()
    {
        I18n.Initialize(new Dictionary<string, string>
        {
            // 状态栏
            ["Status_Ready"] = "就绪",
            ["Status_Loading"] = "正在读取文件信息...",
            ["Status_ListEmpty"] = "列表为空",
            ["Status_ListLoaded"] = "已加载 {0} 个文件",
            ["Status_Cleared"] = "列表已清空",
            ["Status_SkippedFile"] = "跳过不支持的文件：{0}",
            ["Status_RemovedMissing"] = "已清理缺失文件",
            ["Status_Cancelling"] = "正在取消...",

            // 合并状态
            ["Status_MergePreparing"] = "准备合并...",
            ["Status_MergeProgress"] = "正在合并 [{0}/{1}]: {2} ({3} 页)",
            ["Status_MergeComplete"] = "合并完成！共 {0} 页",
            ["Status_MergerSuccess"] = "成功！总页数：{0}，文件：{1}",
            ["Status_MergeFailed"] = "合并失败：{0}",
            ["Status_MergeCancelled"] = "合并已取消",
            ["Status_IgnoreDuplicateFiles"] = "忽略重复文件：{0}",

            // 消息框
            ["Message_Move_Encrypted"] = "已检测到加密文件，无法合并",
            ["Message_EncryptedFiles"] = "以下文件已加密：{0}",
            ["Message_RemovedMissing"] = "已移除 {0} 个缺失文件",
            ["Message_UnsupportedFile"] = "不支持的文件类型：{0}",
            ["Message_InputPasswd"] = "请输入加密文档\"{0}\"的密码：",
            ["Message_InputPasswd_Title"] = "信息输入",
            ["Message_EncryptWarning"] = "警告",
            ["Message_WrongPassword_Retry"] = "密码错误，是否重试？{0}",
            ["Message_InspectFailed"] = "检测失败：{0} {1}",
            ["Message_DecryptingLargeFile"] = "解密大文件：{0}",
            ["Error_BiggerThanMaxSize"] = "文件超过 2GB：{0}",
        });
    }
}

