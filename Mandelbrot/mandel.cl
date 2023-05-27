#pragma pack(push, 1)
typedef struct {
    char B;
    char G;
    char R;
} Pixel;
#pragma pack(pop)

double Map(int value, int inputMin, int inputMax, double outputMin, double outputMax)
{
    return ((value - inputMin) * (outputMax - outputMin) / (inputMax - inputMin)) + outputMin;
}

Pixel HsvToRgb(double hue, double saturation, double value)
{
    int hi = convert_int(round(floor(hue * 6))) % 6;
    double f = hue * 6 - floor(hue * 6);
    double p = value * (1 - saturation);
    double q = value * (1 - f * saturation);
    double t = value * (1 - (1 - f) * saturation);

    double r, g, b;
    switch (hi)
    {
        case 0:
            r = value;
            g = t;
            b = p;
            break;
        case 1:
            r = q;
            g = value;
            b = p;
            break;
        case 2:
            r = p;
            g = value;
            b = t;
            break;
        case 3:
            r = p;
            g = q;
            b = value;
            break;
        case 4:
            r = t;
            g = p;
            b = value;
            break;
        case 5:
            r = value;
            g = p;
            b = q;
            break;
        default:
            r = g = b = 0; // Default to black
            break;
    }

    char red = (char)(r * 255);
    char green = (char)(g * 255);
    char blue = (char)(b * 255);

    return (Pixel){.R = red, .G = green, .B = blue};
}

__kernel void CalculatePixels(__global Pixel* result, int width, int height, double xmin, double ymin, double xmax, double ymax, int depth)
{
    __private int x = get_global_id(0);
    __private int y = get_global_id(1);
    __private int offset = x + width * y;

    __private double zx, zy, cx, cy;
    zx = zy = 0;
    cx = Map(x, 0, width, xmin, xmax);
    cy = Map(y, 0, height, ymin, ymax);

    __private int iteration = 0;
    while (zx * zx + zy * zy < 4 && iteration < depth)
    {
        __private double temp = zx * zx - zy * zy + cx;
        zy = 2 * zx * zy + cy;
        zx = temp;
        iteration++;
    }

    if (iteration == depth)
    {
        result[offset] = (Pixel){.R = 0, .G = 0,.B = 0};
        return;
    }

    __private double smoothColor = iteration + 1 - log(log(sqrt(zx * zx + zy * zy))) / log(2.);
    __private double hue = smoothColor / depth;

    result[offset] = HsvToRgb(hue, 1, 1);
}