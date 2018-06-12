using System;

namespace Soei.Triton2.Common.Infrastructure
{
	[Flags]
	public enum MessageStatus
	{
		Unhandled = 0x0,
		Handled = 0x1,
		MarkedForDeletion = 0x2,
		Complete = Handled | MarkedForDeletion
	};
}