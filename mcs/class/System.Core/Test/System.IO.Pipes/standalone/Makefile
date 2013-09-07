MONO = ../../../../../../mono/mini/mono
MCS = $(MONO) ../../../../../mcs/mcs.exe
MCS_TEST = $(MCS) /target:exe PipeTestWrapper.cs
MONO_TEST = $(MONO) /outfile:$@.log

test: run_test_system_io_pipes_standalone_T100_main \
			run_test_system_io_pipes_standalone_T101_main \
			run_test_system_io_pipes_standalone_T102_main \
			run_test_system_io_pipes_standalone_T103_main \
			run_test_system_io_pipes_standalone_T104_main

test_system_io_pipes_standalone_T100_client: T100_client.cs
	$(MCS_TEST) /out:$@ T100_client.cs

test_system_io_pipes_standalone_T100_main: test_system_io_pipes_standalone_T100_client T100_main.cs
	$(MCS_TEST) /out:$@ T100_main.cs

run_test_system_io_pipes_standalone_T100_main: test_system_io_pipes_standalone_T100_main
	$(MONO) test_system_io_pipes_standalone_T100_main /client:test_system_io_pipes_standalone_T100_client

test_system_io_pipes_standalone_T101_main: T101_main.cs
	$(MCS_TEST) /out:$@ T101_main.cs

run_test_system_io_pipes_standalone_T101_main: test_system_io_pipes_standalone_T101_main
	$(MONO) test_system_io_pipes_standalone_T101_main
	
test_system_io_pipes_standalone_T102_main: T102_main.cs
	$(MCS_TEST) /out:$@ T102_main.cs

run_test_system_io_pipes_standalone_T102_main: test_system_io_pipes_standalone_T102_main
	$(MONO) test_system_io_pipes_standalone_T102_main

test_system_io_pipes_standalone_T103_main: T103_main.cs
	$(MCS_TEST) /out:$@ T103_main.cs

run_test_system_io_pipes_standalone_T103_main: test_system_io_pipes_standalone_T103_main
	$(MONO) test_system_io_pipes_standalone_T103_main

test_system_io_pipes_standalone_T104_main: T104_main.cs
	$(MCS_TEST) /out:$@ T104_main.cs

run_test_system_io_pipes_standalone_T104_main: test_system_io_pipes_standalone_T104_main
	$(MONO) test_system_io_pipes_standalone_T104_main

.PHONY: test run_test_system_io_pipes_standalone_T100_main
# vim: tabstop=2: noexpandtab
