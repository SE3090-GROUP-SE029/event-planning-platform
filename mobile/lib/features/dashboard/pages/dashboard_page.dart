import 'package:flutter/material.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/pastel_card.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  int _currentNavIndex = 0;
  int _selectedDate = 15;

  final List<Map<String, dynamic>> _events = [
    {
      'title': 'Global Tech Summit 2026',
      'category': 'Keynote & Exhibition',
      'time': '09:15 AM',
      'color': AppColors.pastelPink,
      'textColor': AppColors.pastelPinkText,
      'code': 'EVT-7821',
      'coordinator': 'Dr. Everly',
      'status': 'On Schedule',
    },
    {
      'title': 'Samantha & David Wedding',
      'category': 'Reception & Banquet',
      'time': '09:35 AM',
      'color': AppColors.pastelBlue,
      'textColor': AppColors.pastelBlueText,
      'code': 'EVT-9043',
      'coordinator': 'Elena Vance',
      'status': 'Final Review',
    },
    {
      'title': 'Healthcare Innovation Forum',
      'category': 'Virtual Workshop',
      'time': '11:00 AM',
      'color': AppColors.pastelLavender,
      'textColor': AppColors.pastelLavenderText,
      'code': 'EVT-5512',
      'coordinator': 'Marcus Reed',
      'status': 'Ready',
    },
    {
      'title': 'Spring Symphony Gala',
      'category': 'Concert & Dinner',
      'time': '01:45 PM',
      'color': AppColors.pastelGreen,
      'textColor': AppColors.pastelGreenText,
      'code': 'EVT-3329',
      'coordinator': 'Sarah Jenkins',
      'status': 'Setup Pending',
    },
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.canvas,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        title: Row(
          children: [
            Container(
              width: 38,
              height: 38,
              decoration: const BoxDecoration(
                color: AppColors.obsidianBlack,
                shape: BoxShape.circle,
              ),
              child: const Center(
                child: Text(
                  'P',
                  style: TextStyle(
                    color: AppColors.pastelPink,
                    fontWeight: FontWeight.bold,
                    fontSize: 16,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            const Text(
              'Plan It',
              style: TextStyle(
                color: AppColors.textPrimary,
                fontWeight: FontWeight.bold,
                fontSize: 20,
                letterSpacing: -0.5,
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            onPressed: () {},
            icon: Container(
              padding: const EdgeInsets.all(8),
              decoration: const BoxDecoration(
                color: AppColors.obsidianBlack,
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.notifications_none,
                  size: 18, color: Colors.white),
            ),
          ),
          IconButton(
            onPressed: () {
              Navigator.of(context).pushNamed('/test');
            },
            icon: Container(
              padding: const EdgeInsets.all(8),
              decoration: const BoxDecoration(
                color: AppColors.obsidianBlack,
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.science_outlined,
                  size: 18, color: Colors.white),
            ),
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // 1. Search Bar with Pastel Filter Button
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(999),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.03),
                    blurRadius: 12,
                    offset: const Offset(0, 3),
                  ),
                ],
              ),
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
              child: Row(
                children: [
                  Container(
                    width: 36,
                    height: 36,
                    decoration: const BoxDecoration(
                      color: AppColors.pastelPink,
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.search,
                      color: AppColors.obsidianBlack,
                      size: 20,
                    ),
                  ),
                  const SizedBox(width: 12),
                  const Expanded(
                    child: TextField(
                      decoration: InputDecoration(
                        hintText: 'Search events, venues...',
                        hintStyle:
                            TextStyle(color: AppColors.textMuted, fontSize: 13),
                        border: InputBorder.none,
                        enabledBorder: InputBorder.none,
                        focusedBorder: InputBorder.none,
                        contentPadding: EdgeInsets.zero,
                        filled: false,
                      ),
                    ),
                  ),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                    decoration: BoxDecoration(
                      color: AppColors.obsidianBlack,
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: const Text(
                      'All',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  const SizedBox(width: 6),
                ],
              ),
            ),

            const SizedBox(height: 24),

            // 2. Greeting Header
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Good morning, Olivia',
                      style: TextStyle(
                        fontSize: 22,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                        letterSpacing: -0.4,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '45 bookings pending review today',
                      style: TextStyle(
                        fontSize: 13,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
                TextButton(
                  onPressed: () {},
                  child: const Text(
                    'Show all →',
                    style: TextStyle(
                      color: AppColors.textPrimary,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 18),

            // 3. Horizontal Pastel Cards Carousel
            SizedBox(
              height: 175,
              child: ListView(
                scrollDirection: Axis.horizontal,
                clipBehavior: Clip.none,
                children: [
                  // Card 1: Yellow - Bookings
                  PastelCard(
                    width: 250,
                    backgroundColor: AppColors.pastelYellow,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Bookings:',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.pastelYellowText,
                          ),
                        ),
                        const Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('14 pers',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelYellowText)),
                                Text('20-30 YO',
                                    style: TextStyle(
                                        fontSize: 10,
                                        color: Color(0xFF6D5B18))),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('5 pers',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelYellowText)),
                                Text('30-40 YO',
                                    style: TextStyle(
                                        fontSize: 10,
                                        color: Color(0xFF6D5B18))),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('2 pers',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelYellowText)),
                                Text('40+ YO',
                                    style: TextStyle(
                                        fontSize: 10,
                                        color: Color(0xFF6D5B18))),
                              ],
                            ),
                          ],
                        ),
                        // Mini bar visualization
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [16, 28, 12, 38, 24, 14, 20].map((h) {
                            final isDark = h == 38;
                            return Container(
                              width: 6,
                              height: h.toDouble(),
                              decoration: BoxDecoration(
                                color: isDark
                                    ? AppColors.obsidianBlack
                                    : const Color(0xFFE8CA65),
                                borderRadius: BorderRadius.circular(999),
                              ),
                            );
                          }).toList(),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 14),

                  // Card 2: Pink - Visits & Budget
                  PastelCard(
                    width: 260,
                    backgroundColor: AppColors.pastelPink,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Visits summary:',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.pastelPinkText,
                          ),
                        ),
                        const Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('24 min',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelPinkText)),
                                Text('AVERAGE',
                                    style: TextStyle(
                                        fontSize: 9, color: Color(0xFF7A2C55))),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('15 min',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelPinkText)),
                                Text('MINIMUM',
                                    style: TextStyle(
                                        fontSize: 9, color: Color(0xFF7A2C55))),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('01:30 h',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelPinkText)),
                                Text('MAXIMUM',
                                    style: TextStyle(
                                        fontSize: 9, color: Color(0xFF7A2C55))),
                              ],
                            ),
                          ],
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: Colors.white.withOpacity(0.5),
                            borderRadius: BorderRadius.circular(999),
                          ),
                          child: const Text(
                            '↑ 14% vs last week',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: AppColors.pastelPinkText,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 14),

                  // Card 3: Green - Categories
                  PastelCard(
                    width: 250,
                    backgroundColor: AppColors.pastelGreen,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'By Category:',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.pastelGreenText,
                          ),
                        ),
                        const Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('14 items',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelGreenText)),
                                Text('STABLE',
                                    style: TextStyle(
                                        fontSize: 9, color: Color(0xFF42613B))),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('5 items',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelGreenText)),
                                Text('MEDIUM',
                                    style: TextStyle(
                                        fontSize: 9, color: Color(0xFF42613B))),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('1 item',
                                    style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: AppColors.pastelGreenText)),
                                Text('CRITICAL',
                                    style: TextStyle(
                                        fontSize: 9, color: Color(0xFF42613B))),
                              ],
                            ),
                          ],
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: Colors.white.withOpacity(0.5),
                            borderRadius: BorderRadius.circular(999),
                          ),
                          child: const Text(
                            '92% Readiness Index',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: AppColors.pastelGreenText,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 28),

            // 4. Horizontal Date Strip
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'May 2026',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppColors.obsidianBlack,
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: const Text(
                    'Today',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 12),

            SizedBox(
              height: 60,
              child: ListView.builder(
                scrollDirection: Axis.horizontal,
                itemCount: 7,
                itemBuilder: (context, index) {
                  final day = 13 + index;
                  final isSelected = day == _selectedDate;
                  const days = ['WE', 'TH', 'FR', 'SA', 'SU', 'MO', 'TU'];

                  return GestureDetector(
                    onTap: () {
                      setState(() {
                        _selectedDate = day;
                      });
                    },
                    child: Container(
                      width: 48,
                      margin: const EdgeInsets.only(right: 10),
                      decoration: BoxDecoration(
                        color: isSelected ? AppColors.pastelPink : Colors.white,
                        borderRadius: BorderRadius.circular(16),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.02),
                            blurRadius: 8,
                          ),
                        ],
                      ),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text(
                            days[index],
                            style: TextStyle(
                              fontSize: 10,
                              fontWeight: FontWeight.bold,
                              color: isSelected
                                  ? AppColors.pastelPinkText
                                  : AppColors.textMuted,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            '$day',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: isSelected
                                  ? AppColors.pastelPinkText
                                  : AppColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),

            const SizedBox(height: 28),

            // 5. Schedule & Events List
            const Text(
              "Schedule & Events",
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),

            const SizedBox(height: 14),

            ListView.separated(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: _events.length,
              separatorBuilder: (context, index) => const SizedBox(height: 10),
              itemBuilder: (context, index) {
                final item = _events[index];
                return Container(
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.02),
                        blurRadius: 10,
                        offset: const Offset(0, 2),
                      ),
                    ],
                  ),
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      Container(
                        width: 44,
                        height: 44,
                        decoration: BoxDecoration(
                          color: item['color'] as Color,
                          shape: BoxShape.circle,
                        ),
                        child: Center(
                          child: Text(
                            (item['title'] as String).substring(0, 1),
                            style: TextStyle(
                              color: item['textColor'] as Color,
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              item['title'] as String,
                              style: const TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.bold,
                                color: AppColors.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              item['category'] as String,
                              style: const TextStyle(
                                fontSize: 12,
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 5),
                        decoration: BoxDecoration(
                          color: AppColors.canvas,
                          borderRadius: BorderRadius.circular(999),
                        ),
                        child: Text(
                          item['time'] as String,
                          style: const TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),

            const SizedBox(height: 80), // Padding for floating button
          ],
        ),
      ),

      // Floating Action Button - High Contrast Obsidian Pill
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {},
        backgroundColor: AppColors.obsidianBlack,
        foregroundColor: Colors.white,
        elevation: 4,
        icon: const Icon(Icons.add, size: 20),
        label: const Text(
          'New Event',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
        ),
      ),

      // Bottom Navigation Bar - Floating Rounded Design
      bottomNavigationBar: Container(
        margin: const EdgeInsets.only(left: 20, right: 20, bottom: 20),
        decoration: BoxDecoration(
          color: AppColors.obsidianBlack,
          borderRadius: BorderRadius.circular(30),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.15),
              blurRadius: 20,
              offset: const Offset(0, 8),
            ),
          ],
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(30),
          child: BottomNavigationBar(
            currentIndex: _currentNavIndex,
            onTap: (index) {
              setState(() {
                _currentNavIndex = index;
              });
            },
            backgroundColor: Colors.transparent,
            elevation: 0,
            selectedItemColor: AppColors.pastelPink,
            unselectedItemColor: Colors.white.withOpacity(0.5),
            type: BottomNavigationBarType.fixed,
            showSelectedLabels: false,
            showUnselectedLabels: false,
            items: const [
              BottomNavigationBarItem(
                icon: Icon(Icons.dashboard_outlined),
                activeIcon: Icon(Icons.dashboard),
                label: 'Dashboard',
              ),
              BottomNavigationBarItem(
                icon: Icon(Icons.calendar_today_outlined),
                activeIcon: Icon(Icons.calendar_today),
                label: 'Calendar',
              ),
              BottomNavigationBarItem(
                icon: Icon(Icons.people_outline),
                activeIcon: Icon(Icons.people),
                label: 'Events',
              ),
              BottomNavigationBarItem(
                icon: Icon(Icons.settings_outlined),
                activeIcon: Icon(Icons.settings),
                label: 'Settings',
              ),
            ],
          ),
        ),
      ),
    );
  }
}
