import socket
import struct

sock = socket.socket()
data = bytearray(48)
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.sendto(struct.pack("i", 3), 0, ('127.0.0.1', 123))
