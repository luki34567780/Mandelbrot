import imageio
from tqdm import tqdm

# Define the path and filename pattern of the bitmaps
bitmap_path = 'bin/Debug/net7.0/'
filename_pattern = '{}.bmp'

# Define the output video file name
output_file = 'output.mp4'

# Create a list to store the image frames
frames = []

# Read and append each bitmap to the frames list
for i in tqdm(range(1, 1200)):
    filename = filename_pattern.format(i)
    filepath = bitmap_path + filename
    image = imageio.imread(filepath)
    frames.append(image)

# Save the frames as an MP4 video using imageio
imageio.mimsave(output_file, frames, fps=30)

print("Video created successfully.")