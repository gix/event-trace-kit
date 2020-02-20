all:

regenerate:
	mc -um -z x ImportChannels.man && \
		move xTEMP.BIN ImportChannels.wevt.bin && \
		move x_MSG00001.bin ImportChannels.msg.bin

	mc -um -z x ImportChannelFromProvider.man && \
		move xTEMP.BIN ImportChannelFromProvider.wevt.bin && \
		move x_MSG00001.bin ImportChannelFromProvider.msg.bin

	mc -um -z x ReferenceChannels.man && \
		move xTEMP.BIN ReferenceChannels.wevt.bin && \
		move x_MSG00001.bin ReferenceChannels.msg.bin

	mc -um -z x TaskOpcodes.man && \
		move xTEMP.BIN TaskOpcodes.wevt.bin && \
		move x_MSG00001.bin TaskOpcodes.msg.bin

	mc -um -z x wpf-etw.man && \
		move xTEMP.BIN wpf-etw.wevt.bin && \
		move x_MSG00001.bin wpf-etw.msg.bin
