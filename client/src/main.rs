use std::{
    io::{Cursor, Read, Write},
    net::TcpStream,
};

use image::{DynamicImage, ImageError, Rgb};

static SERVER_IP: &str = "0.0.0.0:42069";

fn process_img(data: &[u8], out_buf: &mut Vec<u8>) -> Result<(), ImageError> {
    let img = image::load_from_memory(data)?;
    let mut img = img.to_rgb8();

    for pixel in img.pixels_mut() {
        *pixel = Rgb([255 - pixel[0], 255 - pixel[1], 255 - pixel[2]]);
    }

    let mut cursor = Cursor::new(out_buf);
    DynamicImage::ImageRgb8(img).write_to(&mut cursor, image::ImageFormat::Png)?;

    Ok(())
}

struct Client {
    stream: TcpStream,
    len_buffer: [u8; 4],
    buffer: Vec<u8>,
    out_buffer: Vec<u8>,
}

impl Client {
    pub fn new(ip: &str) -> std::io::Result<Self> {
        Ok(Self {
            stream: TcpStream::connect(ip)?,
            len_buffer: [0; 4],
            buffer: Vec::new(),
            out_buffer: Vec::new(),
        })
    }

    pub fn client_loop(&mut self) -> std::io::Result<()> {
        loop {
            match self.stream.read_exact(&mut self.len_buffer) {
                Ok(_) => (),
                Err(e) if e.kind() == std::io::ErrorKind::UnexpectedEof => return Ok(()),
                Err(e) => return Err(e),
            }
            let len = u32::from_be_bytes(self.len_buffer) as usize;

            self.buffer.resize(len, 0);
            self.stream.read_exact(&mut self.buffer)?;

            if let Err(e) = process_img(&self.buffer, &mut self.out_buffer) {
                eprintln!("Error processing img: {e}");
                break;
            };

            let out_len = self.out_buffer.len() as u32;
            self.stream.write_all(&out_len.to_be_bytes())?;
            self.stream.write_all(&self.out_buffer)?;

            self.buffer.clear();
            self.out_buffer.clear();
        }

        Ok(())
    }
}

fn main() -> std::io::Result<()> {
    let mut client = Client::new(SERVER_IP)?;
    client.client_loop()?;
    Ok(())
}
