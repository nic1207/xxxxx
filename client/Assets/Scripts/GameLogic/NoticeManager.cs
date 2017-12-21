using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Mogo.Util;
using System;

public class NoticeManager
{
    private static NoticeManager m_instance;

    public static NoticeManager Instance
    {
        get { return m_instance; }
        set { m_instance = value; }
    }

    private NoticeManager() { }

    static NoticeManager()
    {
        m_instance = new NoticeManager();
    }

    public string noticeXml = string.Empty;
    public bool IsDownloadFinished { get; private set; }
    public Action DownloadFinished;

    public void AutoShowNotice()
    {
        MogoNotice2.Instance.PreloadResource();
        if (IsDownloadFinished)
            ShowNotice();
        else
            DownloadFinished = ShowNotice;
    }

    public void ShowNotice()
    {
        if (String.IsNullOrEmpty(noticeXml))
        {
            Debug.Log("Empty Notice.");
            return;
        }
        try
        {
            var xml = XMLParser.LoadXML(noticeXml);
            var map = XMLParser.LoadIntMap(xml, SystemConfig.NOTICE_CONTENT_KEY);

            List<MogoNotice2.Notice> list = new List<MogoNotice2.Notice>();
            foreach (Dictionary<string, string> dic in map.Values)
            {
                MogoNotice2.Notice notice = new MogoNotice2.Notice();
                notice.text = dic["text"];
                notice.title = dic["title"];
                notice.date = dic["date"];
                notice.isNew = (dic["isnew"] == "1" ? true : false);
                list.Add(notice);

                //Debug.Log("date:" + notice.date + ",title:" + notice.title + ",text:" + notice.text + ",isnew:" + notice.isNew);
            }
            list.Reverse();

            MogoNotice2.Instance.ShowNotice(list);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public IEnumerator DownloadNotice()
    {
		string url = SystemConfig.GetCfgInfoUrl (SystemConfig.NOTICE_URL_KEY);
		if (url == string.Empty) {
			url = "http://192.168.20.107/mogo/notice.xml";
		}

		WWW d = new WWW (url);
		//Debug.Log("DownloadNotice() url="+url);

        yield return d;
        noticeXml = d.text;
        //Debug.Log(d.text);
#if UNITY_IPHONE
		//use native md5 interface
		string md5 = IOSUtils.FormatMD5(IOSUtils.CreateMD5(d.bytes));
#else
        string md5 = Utils.FormatMD5(Utils.CreateMD5(d.bytes));
#endif
        //Debug.Log("md5:" + md5);
        if (SystemConfig.Instance.noticeMd5 == md5 && SystemConfig.Instance.Passport == SystemConfig.Instance.PassportSeenNotice)
        {
            yield break;
        }
        SystemConfig.Instance.noticeMd5 = md5;
        SystemConfig.Instance.PassportSeenNotice = SystemConfig.Instance.Passport;
        SystemConfig.SaveConfig();
        if (DownloadFinished != null && !MogoWorld.BeginLogin)//如果开始登录了就不打开公告板
            DownloadFinished();
        IsDownloadFinished = true;
    }
}