using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 邮件数据配置（ScriptableObject）
/// 存储所有可用的邮件按钮数据（按钮池）
/// </summary>
[CreateAssetMenu(fileName = "MailData", menuName = "Game/Mail Data")]
public class MailDataConfig : ScriptableObject
{
    [System.Serializable]
    public class MailInfo
    {
        [Header("邮件信息")]
        [Tooltip("邮件唯一ID（建议从0或1开始递增，确保唯一性）")]
        public int mailId;
        
        [Tooltip("解锁这封邮件所需的订单ID")]
        public int unlockOrderId;
        
        [Tooltip("首次添加这封邮件到邮件面板的场景名称（进入该场景时，如果邮件已解锁，则自动添加到PlayerPrefs）")]
        public string firstAddSceneName;
        
        [Tooltip("按钮上显示的图标图片")]
        public Sprite buttonIcon;
        
        [Tooltip("右侧面板显示的对应图片")]
        public Sprite contentImage;
    }
    
    [Header("邮件数据列表")]
    [Tooltip("配置所有邮件数据。前5个邮件为初始显示的邮件，后续邮件按M键时依次解锁显示。")]
    public List<MailInfo> mailDataList = new List<MailInfo>();
    
    /// <summary>
    /// 根据邮件ID获取邮件信息
    /// </summary>
    /// <param name="mailId">邮件ID</param>
    /// <returns>找到的邮件信息，如果不存在返回null</returns>
    public MailInfo GetMailInfoById(int mailId)
    {
        if (mailDataList == null || mailDataList.Count == 0)
        {
            Debug.LogWarning("MailDataConfig: mailDataList为空！请配置邮件数据。");
            return null;
        }
        
        foreach (var info in mailDataList)
        {
            if (info != null && info.mailId == mailId)
            {
                return info;
            }
        }
        
        Debug.LogWarning($"MailDataConfig: 未找到mailId为 {mailId} 的邮件数据！");
        return null;
    }
    
    /// <summary>
    /// 自动填充订单ID（设置为与邮件ID相同）
    /// </summary>
    [ContextMenu("自动填充订单ID")]
    public void AutoFillUnlockOrderIds()
    {
        if (mailDataList == null || mailDataList.Count == 0)
        {
            Debug.LogWarning("MailDataConfig: 邮件数据列表为空！");
            return;
        }
        
        int updatedCount = 0;
        for (int i = 0; i < mailDataList.Count; i++)
        {
            var info = mailDataList[i];
            if (info == null)
            {
                continue;
            }
            
            info.unlockOrderId = info.mailId;
            updatedCount++;
        }
        
        Debug.Log($"MailDataConfig: 已自动填充 {updatedCount} 个邮件的订单ID（设置为与邮件ID相同）");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// 检查数据配置是否完整（用于编辑器验证）
    /// </summary>
    [ContextMenu("验证数据配置")]
    public void ValidateData()
    {
        if (mailDataList == null || mailDataList.Count == 0)
        {
            Debug.LogWarning("MailDataConfig: 邮件数据列表为空！");
            return;
        }
        
        HashSet<int> mailIds = new HashSet<int>();
        
        for (int i = 0; i < mailDataList.Count; i++)
        {
            var info = mailDataList[i];
            if (info == null)
            {
                Debug.LogWarning($"MailDataConfig: 第 {i} 个邮件信息为空！");
                continue;
            }
            
            // 检查mailId唯一性
            if (mailIds.Contains(info.mailId))
            {
                Debug.LogWarning($"MailDataConfig: 第 {i} 个邮件信息的mailId {info.mailId} 与其他邮件重复！");
            }
            else
            {
                mailIds.Add(info.mailId);
            }
            
            if (info.buttonIcon == null)
            {
                Debug.LogWarning($"MailDataConfig: 第 {i} 个邮件信息（mailId={info.mailId}）的按钮图标未配置！");
            }
            
            if (info.contentImage == null)
            {
                Debug.LogWarning($"MailDataConfig: 第 {i} 个邮件信息（mailId={info.mailId}）的内容图片未配置！");
            }
            
            // 检查场景名称配置
            if (string.IsNullOrEmpty(info.firstAddSceneName))
            {
                Debug.LogWarning($"MailDataConfig: 第 {i} 个邮件信息（mailId={info.mailId}）的首添场景名称未配置！");
            }
        }
        
        Debug.Log($"MailDataConfig: 数据验证完成，共 {mailDataList.Count} 个邮件，其中 {mailIds.Count} 个唯一ID");
    }
}
