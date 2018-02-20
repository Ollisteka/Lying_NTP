import socket
import struct
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
a = struct.pack("i", 3)
sock.sendto(a, 0, ('127.0.0.1', 123))
sock.settimeout(10)
data = sock.recv(1024)
print(data)