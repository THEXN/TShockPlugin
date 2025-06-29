using System;
using System.Collections.Generic;
using TShockAPI;

namespace TrCDK;

	public static class TShockCommandExtensions
	{
		public static bool RunWithoutPermissions(this Command cmd, string msg, TSPlayer ply, List<string> parms, bool silent = false)
		{
			try
			{
				var commandDelegate = cmd.CommandDelegate;
				commandDelegate(new CommandArgs(msg, silent, ply, parms));
			}
			catch (Exception ex)
			{
				ply.SendErrorMessage(GetString("ָ��ִ��ʧ�ܣ�����ϵ����Ա"));
				TShock.Log.Error(ex.ToString());
			}
			return true;
		}
	}
