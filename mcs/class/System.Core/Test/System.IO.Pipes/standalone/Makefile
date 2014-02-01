# Configuring MONO_PATH / MONO_CFG_DIR / MONO_CONFIG for all test runs to allow execution from uninstalled binaries
# Configuring PATH variable for all test runs to ensure Process.Start() can resolve managed exes
MONO_PATH = .:../../../../lib/net_4_5
MONO_CFG_DIR = ../../../../../../runtime/etc/mono/4.5
MONO_CONFIG = ../../../../../../runtime/etc/mono/config
MONO_APP_PATH = ../../../../../../mono/mini/
MONO = PATH=$(MONO_APP_PATH) MONO_PATH=$(MONO_PATH) MONO_CFG_DIR=$(MONO_CFG_DIR) MONO_CONFIG=$(MONO_CONFIG) $(MONO_APP_PATH)mono 
#MONO_DBG_FLAGS = --debug --trace
MONO_DBG_FLAGS = --debug 
MONO_TEST = MONO_ENV_OPTIONS='$(MONO_DBG_FLAGS)' $(MONO) 
MCS = $(MONO) ../../../../../mcs/mcs.exe
MCS_TEST = $(MCS) /debug+ /target:exe PipeTestWrapper.cs
LOGDIR = logs/

# Compare latest output to accepted output and fail if any lines (except those starting with [INFO])
# differ from the accepted
COMPARE_OUTPUT=\
	if (test -f $(1) || (echo 'accepted file ' $(1) ' does not exist' && false)) && \
	   (test -f $(2) || (echo 'latest file ' $(2) ' does not exist' && false)); then \
		diff -I '^\[INFO\].*$$' $(1) $(2); \
	else \
		false; \
	fi;

all: test_system_io_pipes_standalone_T100_client \
		 test_system_io_pipes_standalone_T100_main \
     test_system_io_pipes_standalone_T101_main \
		 test_system_io_pipes_standalone_T102_main \
		 test_system_io_pipes_standalone_T103_main \
		 test_system_io_pipes_standalone_T104_main \
		 test_system_io_pipes_standalone_T105_client \
		 test_system_io_pipes_standalone_T105_main \
		 test_system_io_pipes_standalone_T106_client \
		 test_system_io_pipes_standalone_T106_main \
		 test_system_io_pipes_standalone_T107_main \
		 test_system_io_pipes_standalone_T107_client \
		 test_system_io_pipes_standalone_T108_main \
		 test_system_io_pipes_standalone_T108_client \
		 test_system_io_pipes_standalone_T109_main \
		 test_system_io_pipes_standalone_T109_client  \
		 test_system_io_pipes_standalone_T110_main \
		 test_system_io_pipes_standalone_T110_client \
		 test_system_io_pipes_standalone_T111_main \
		 test_system_io_pipes_standalone_T111_client 

test: run_test_system_io_pipes_standalone_T100_main \
			run_test_system_io_pipes_standalone_T101_main \
			run_test_system_io_pipes_standalone_T102_main \
			run_test_system_io_pipes_standalone_T103_main \
			run_test_system_io_pipes_standalone_T104_main \
			run_test_system_io_pipes_standalone_T105_main \
			run_test_system_io_pipes_standalone_T106_main \
			run_test_system_io_pipes_standalone_T107_main \
			run_test_system_io_pipes_standalone_T108_main \
			run_test_system_io_pipes_standalone_T109_main \
			run_test_system_io_pipes_standalone_T110_main \
			run_test_system_io_pipes_standalone_T111_main 

test_system_io_pipes_standalone_T100_client: T100_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T100_client.cs
test_system_io_pipes_standalone_T100_main: test_system_io_pipes_standalone_T100_client T100_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T100_main.cs
run_test_system_io_pipes_standalone_T100_main: test_system_io_pipes_standalone_T100_main
	$(MONO_TEST) test_system_io_pipes_standalone_T100_main /client:test_system_io_pipes_standalone_T100_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T101_main: T101_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T101_main.cs

run_test_system_io_pipes_standalone_T101_main: test_system_io_pipes_standalone_T101_main
	$(MONO_TEST) test_system_io_pipes_standalone_T101_main > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)
	
test_system_io_pipes_standalone_T102_main: T102_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T102_main.cs

run_test_system_io_pipes_standalone_T102_main: test_system_io_pipes_standalone_T102_main
	$(MONO_TEST) test_system_io_pipes_standalone_T102_main > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T103_main: T103_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T103_main.cs

run_test_system_io_pipes_standalone_T103_main: test_system_io_pipes_standalone_T103_main
	$(MONO_TEST) test_system_io_pipes_standalone_T103_main > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T104_main: T104_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T104_main.cs

run_test_system_io_pipes_standalone_T104_main: test_system_io_pipes_standalone_T104_main
	$(MONO_TEST) test_system_io_pipes_standalone_T104_main > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T105_client: T105_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T105_client.cs

test_system_io_pipes_standalone_T105_main: test_system_io_pipes_standalone_T105_client T105_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T105_main.cs

run_test_system_io_pipes_standalone_T105_main: test_system_io_pipes_standalone_T105_main
	$(MONO_TEST) test_system_io_pipes_standalone_T105_main /client:test_system_io_pipes_standalone_T105_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T106_client: T106_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T106_client.cs
test_system_io_pipes_standalone_T106_main: test_system_io_pipes_standalone_T106_client T106_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T106_main.cs
run_test_system_io_pipes_standalone_T106_main: test_system_io_pipes_standalone_T106_main
	$(MONO_TEST) test_system_io_pipes_standalone_T106_main /client:test_system_io_pipes_standalone_T106_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T107_client: T107_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T107_client.cs
test_system_io_pipes_standalone_T107_main: test_system_io_pipes_standalone_T107_client T107_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T107_main.cs
run_test_system_io_pipes_standalone_T107_main: test_system_io_pipes_standalone_T107_main
	$(MONO_TEST) test_system_io_pipes_standalone_T107_main /client:test_system_io_pipes_standalone_T107_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T108_client: T108_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T108_client.cs
test_system_io_pipes_standalone_T108_main: test_system_io_pipes_standalone_T108_client T108_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T108_main.cs
run_test_system_io_pipes_standalone_T108_main: test_system_io_pipes_standalone_T108_main
	$(MONO_TEST) test_system_io_pipes_standalone_T108_main /client:test_system_io_pipes_standalone_T108_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T109_client: T109_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T109_client.cs
test_system_io_pipes_standalone_T109_main: test_system_io_pipes_standalone_T109_client T109_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T109_main.cs
run_test_system_io_pipes_standalone_T109_main: test_system_io_pipes_standalone_T109_main
	$(MONO_TEST) test_system_io_pipes_standalone_T109_main /client:test_system_io_pipes_standalone_T109_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T110_client: T110_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T110_client.cs
test_system_io_pipes_standalone_T110_main: test_system_io_pipes_standalone_T110_client T110_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T110_main.cs
run_test_system_io_pipes_standalone_T110_main: test_system_io_pipes_standalone_T110_main
	$(MONO_TEST) test_system_io_pipes_standalone_T110_main /client:test_system_io_pipes_standalone_T110_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

test_system_io_pipes_standalone_T111_client: T111_client.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T111_client.cs
test_system_io_pipes_standalone_T111_main: test_system_io_pipes_standalone_T111_client T111_main.cs PipeTestWrapper.cs
	$(MCS_TEST) /out:$@ T111_main.cs
run_test_system_io_pipes_standalone_T111_main: test_system_io_pipes_standalone_T111_main
	$(MONO_TEST) test_system_io_pipes_standalone_T111_main /client:test_system_io_pipes_standalone_T111_client > $(LOGDIR)$@.latest
	$(call COMPARE_OUTPUT, $(LOGDIR)$@.accepted, $(LOGDIR)$@.latest)

.PHONY: all test
# vim: tabstop=2: noexpandtab
