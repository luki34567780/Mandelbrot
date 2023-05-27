import pyopencl as cl
import numpy as np

context = cl.create_some_context()

with open("mandel.cl", "r") as f:
    code = f.read()

program = cl.Program(context, code)
program.build()

print(program.kernel_names)
kernel = program.CalculatePixels

width = 1920
height = 1080

result_buffer = cl.Buffer(context, cl.mem_flags.WRITE_ONLY, width * height * 4)
kernel.set_arg(0, result_buffer)  # Argument 0: __global Pixel* result
kernel.set_arg(1, np.int32(width))  # Argument 1: const int width
kernel.set_arg(2, np.int32(height))  # Argument 2: const int height
kernel.set_arg(3, np.int32(-2.5))  # Argument 3: const int xmin
kernel.set_arg(4, np.int32(-1))  # Argument 4: const int ymin
kernel.set_arg(5, np.int32(1))  # Argument 5: const int xmax
kernel.set_arg(6, np.int32(1))  # Argument 6: const int ymax
kernel.set_arg(7, np.int32(1000))  # Argument 7: const int depth

global_size = (width, height)
local_size = None  # You can specify local_size if desired
queue = cl.CommandQueue(context)
cl.enqueue_nd_range_kernel(queue, kernel, global_size, local_size)
#queue.enqueue_nd_range_kernel(kernel, global_size, local_size)

result = np.empty(width * height * 4, dtype=np.ubyte)
cl.enqueue_copy(queue, result, result_buffer)


while True:
    pass