import socket
import struct
import numpy as np
from PIL import Image
from io import BytesIO

def split_image(image: np.ndarray, n: int):
    h, w = image.shape[:2]
    part_h = h // n
    part_w = w // n
    subimages = []

    for i in range(n):
        for j in range(n):
            sub = image[i * part_h:(i + 1) * part_h,
                        j * part_w:(j + 1) * part_w]
            x, y = j * part_w, i * part_h
            subimages.append((x, y, sub))
    return subimages, (w, h)

def scal(size, tiles):
    out = Image.new("RGB", size)
    for x, y, tile in tiles:
        if isinstance(tile, np.ndarray):
            tile = Image.fromarray(tile)
        out.paste(tile, (x, y))
    return out

def recv_data(conn):
    raw_len = conn.recv(4)
    if not raw_len:
        return None
    length = struct.unpack('!I', raw_len)[0]
    data = b''
    while len(data) < length:
        print("huh")
        packet = conn.recv(length - len(data))
        if not packet:
            return None
        data += packet
    return data

def send_data(conn, data: bytes):
    length = struct.pack('!I', len(data))
    conn.sendall(length)
    print(len(data))
    conn.sendall(data)

def encode_tile_png(tile: np.ndarray) -> bytes:
    buf = BytesIO()
    Image.fromarray(tile).save(buf, format="PNG")
    return buf.getvalue()

def decode_tile_png(data: bytes) -> np.ndarray:
    return np.array(Image.open(BytesIO(data)))


HOST = '0.0.0.0'
PORT = 42069
n_parts = 2

image = np.array(Image.open("zdj.jpg").convert("RGB"))
parts, (w, h) = split_image(image, n_parts)
total_parts = len(parts)
processed_parts = []
running = True

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
    server.bind((HOST, PORT))
    server.listen()
    print(f"listening on {HOST}:{PORT}")

    part_idx = 0

    while running:
        conn, addr = server.accept()
        with conn:
            print(f"connected by {addr}")
            while part_idx < total_parts:
                x, y, tile = parts[part_idx]
                png_bytes = encode_tile_png(tile)
                send_data(conn, png_bytes)
                print(f"sent tile {part_idx+1}/{total_parts}")

                received = recv_data(conn)
                if received is None:
                    break
                processed_tile = decode_tile_png(received)
                processed_parts.append((x, y, processed_tile))
                print(f"received processed tile {part_idx+1}/{total_parts}")

                part_idx += 1

                if len(processed_parts) == total_parts:
                    final_image = scal((w, h), processed_parts)
                    final_image.save("result.png")
                    print("processed image saved as result.png")
                    running = False
