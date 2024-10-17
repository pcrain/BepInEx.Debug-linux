RM := rm -rf
MKDIR_P = mkdir -p

NOWARN := -Wno-unused-variable -Wno-format-truncation

OBJS += \
build/dllmain.o

CPP_DEPS += \
build/dllmain.d

# LIBS := -ljsoncpp -lncurses -lnotify -lgtk-3 -lglib-2.0 -lgobject-2.0 -ltinfo

# INCLUDES = \
# 	-I/usr/include/gtk-3.0

BUILD_DIR = build

all: directories MonoProfiler64

MonoProfiler64: $(OBJS)
	@echo 'Building target: $@'
	@echo 'Invoking: GCC C++ Linker'
	g++ -rdynamic -L/usr/lib -std=c++17 -o "./MonoProfiler64.so" $(OBJS) $(LIBS) -ldl -shared
	@echo 'Finished building target: $@'
	@echo ' '

build/%.o: ./%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -rdynamic -D__GXX_EXPERIMENTAL_CXX0X__ $(INCLUDES) -O3 -g3 -Wall -c -fmessage-length=0 -std=c++17 $(NOWARN)  -fPIC -o "$@" "$<" -ldl
	@echo 'Finished building: $<'
	@echo ' '

clean:
	-$(RM) $(OBJS)$(C++_DEPS) ./MonoProfiler
	-@echo ' '

directories: ${BUILD_DIR}

${BUILD_DIR}:
	${MKDIR_P} ${BUILD_DIR}

.PHONY: all clean directories
.SECONDARY:
