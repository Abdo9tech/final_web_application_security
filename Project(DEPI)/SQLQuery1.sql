INSERT INTO [dbo].[Rooms] ([RoomNumber], [Floor], [Status], [RoomTypeId], [IsAvailable], [ImageUrl], [Location]) VALUES
-- Floor 1: Standard Rooms
(101, 1, 'Available', 1, 1, 'https://images.unsplash.com/photo-1631049307264-da0ec9d70304?auto=format&fit=crop&w=800', 'United States'),
(102, 1, 'Available', 1, 1, 'https://images.unsplash.com/photo-1584132967334-10e028bd69f7?auto=format&fit=crop&w=800', 'United Kingdom'),
(103, 1, 'Available', 2, 1, 'https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800', 'France'),
(104, 1, 'Maintenance', 2, 0, 'https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=800', 'Germany'),
(105, 1, 'Available', 9, 1, 'https://images.unsplash.com/photo-1584132915807-fd1f5fbc078f?auto=format&fit=crop&w=800', 'Japan'),

-- Floor 2: Deluxe Rooms
(201, 2, 'Available', 3, 1, 'https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=800', 'Italy'),
(202, 2, 'Available', 3, 1, 'https://images.unsplash.com/photo-1615873968403-89e068629265?auto=format&fit=crop&w=800', 'Spain'),
(203, 2, 'Occupied', 4, 0, 'https://images.unsplash.com/photo-1497366754035-f200968a6e72?auto=format&fit=crop&w=800', 'United Arab Emirates'),
(204, 2, 'Available', 4, 1, 'https://images.unsplash.com/photo-1631049421452-4c0f1c5c0185?auto=format&fit=crop&w=800', 'Switzerland'),
(205, 2, 'Available', 5, 1, 'https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800', 'Canada'),

-- Floor 3: Premium Rooms
(301, 3, 'Available', 6, 1, 'https://images.unsplash.com/photo-1560448204-603b3fc33ddc?auto=format&fit=crop&w=800', 'Singapore'),
(302, 3, 'Available', 7, 1, 'https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?auto=format&fit=crop&w=800', 'Greece'),
(303, 3, 'Available', 7, 1, 'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=800', 'Australia'),
(304, 3, 'Cleaning', 8, 0, 'https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800', 'Netherlands'),
(305, 3, 'Available', 10, 1, 'https://images.unsplash.com/photo-1591088398330-8d4c6c3d2c4d?auto=format&fit=crop&w=800', 'Sweden'),

-- Floor 4: Additional Rooms
(401, 4, 'Available', 3, 1, 'https://images.unsplash.com/photo-1631049421452-4c0f1c5c0185?auto=format&fit=crop&w=800', 'Brazil'),
(402, 4, 'Available', 2, 1, 'https://images.unsplash.com/photo-1631049307264-da0ec9d70304?auto=format&fit=crop&w=800', 'Mexico'),
(403, 4, 'Available', 1, 1, 'https://images.unsplash.com/photo-1584132967334-10e028bd69f7?auto=format&fit=crop&w=800', 'Thailand'),
(404, 4, 'Available', 5, 1, 'https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=800', 'South Africa'),
(405, 4, 'Available', 9, 1, 'https://images.unsplash.com/photo-1584132915807-fd1f5fbc078f?auto=format&fit=crop&w=800', 'South Korea');

