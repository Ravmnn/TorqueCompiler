	.file	"MainModule"
	.text
	.globl	abc                             # -- Begin function abc
	.p2align	4
	.type	abc,@function
abc:                                    # @abc
	.cfi_startproc
# %bb.0:                                # %entry
	movl	$20, %eax
	retq
.Lfunc_end0:
	.size	abc, .Lfunc_end0-abc
	.cfi_endproc
                                        # -- End function
	.section	".note.GNU-stack","",@progbits
