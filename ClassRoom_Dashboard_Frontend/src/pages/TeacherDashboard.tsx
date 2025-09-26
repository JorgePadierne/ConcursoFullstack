import React, { useEffect, useState } from "react";
import { useAuth } from "../contexts/AuthContext";
import apiClient from "../utils/apiClient";
import type { TeacherCourseSummary, Course } from "../types/api";
import {
  ChartBarIcon,
  BookOpenIcon,
  UsersIcon,
  ClipboardDocumentCheckIcon,
  AcademicCapIcon,
} from "@heroicons/react/24/outline";

const TeacherDashboard: React.FC = () => {
  const { user } = useAuth();
  const [courseSummaries, setCourseSummaries] = useState<
    TeacherCourseSummary[]
  >([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const [dashboardResponse, coursesResponse] = await Promise.all([
          apiClient.getTeacherDashboard(),
          apiClient.getCourses(),
        ]);

        setCourseSummaries(dashboardResponse.data);
        setCourses(coursesResponse.data);
      } catch (error) {
        console.error("Failed to fetch dashboard data:", error);
      } finally {
        setLoading(false);
      }
    };

    if (user?.role === "profesor") {
      fetchDashboardData();
    }
  }, [user]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  const totalTasks = courseSummaries.reduce(
    (sum, course) => sum + course.tasks,
    0
  );
  const totalSubmissions = courseSummaries.reduce(
    (sum, course) => sum + course.submissions,
    0
  );
  const totalCompleted = courseSummaries.reduce(
    (sum, course) => sum + course.handedIn,
    0
  );
  const overallCompletionRate =
    totalTasks > 0 ? (totalCompleted / totalTasks) * 100 : 0;

  return (
    <div className="space-y-6">
      {/* Welcome Header */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
          Welcome back, Professor {user?.name || user?.email}!
        </h1>
        <p className="mt-1 text-gray-600 dark:text-gray-300">
          Here's an overview of your courses and student progress
        </p>
      </div>

      {/* Overview Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <BookOpenIcon className="h-8 w-8 text-blue-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-300">
                Active Courses
              </p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {courses.length}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <ClipboardDocumentCheckIcon className="h-8 w-8 text-green-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-300">
                Total Assignments
              </p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {totalTasks}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <UsersIcon className="h-8 w-8 text-purple-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-300">
                Total Submissions
              </p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {totalSubmissions}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <ChartBarIcon className="h-8 w-8 text-orange-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-300">
                Completion Rate
              </p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {overallCompletionRate.toFixed(1)}%
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Course Performance */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
            Course Performance
          </h3>
        </div>
        <div className="divide-y divide-gray-200 dark:divide-gray-700">
          {courseSummaries.length > 0 ? (
            courseSummaries.map((course) => {
              const completionRate =
                course.tasks > 0 ? (course.handedIn / course.tasks) * 100 : 0;
              const courseInfo = courses.find((c) => c.id === course.courseId);

              return (
                <div key={course.courseId} className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <div className="flex items-center">
                        <AcademicCapIcon className="h-5 w-5 text-blue-600 mr-3" />
                        <div>
                          <h4 className="text-sm font-medium text-gray-900 dark:text-white">
                            {courseInfo?.name || course.courseId}
                          </h4>
                          <p className="text-sm text-gray-500 dark:text-gray-300">
                            {courseInfo?.section}
                          </p>
                        </div>
                      </div>
                      <div className="mt-3 grid grid-cols-3 gap-4">
                        <div>
                          <p className="text-xs text-gray-500 dark:text-gray-300">
                            Assignments
                          </p>
                          <p className="text-sm font-medium text-gray-900 dark:text-white">
                            {course.tasks}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs text-gray-500 dark:text-gray-300">
                            Submissions
                          </p>
                          <p className="text-sm font-medium text-gray-900 dark:text-white">
                            {course.submissions}
                          </p>
                        </div>
                        <div>
                          <p className="text-xs text-gray-500 dark:text-gray-300">
                            Completed
                          </p>
                          <p className="text-sm font-medium text-gray-900 dark:text-white">
                            {course.handedIn}
                          </p>
                        </div>
                      </div>
                    </div>
                    <div className="ml-6">
                      <div className="text-right">
                        <p className="text-sm font-medium text-gray-900 dark:text-white">
                          {completionRate.toFixed(1)}%
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-300">
                          completion
                        </p>
                      </div>
                      <div className="mt-2 w-20 bg-gray-200 dark:bg-gray-700 rounded-full h-2">
                        <div
                          className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                          style={{ width: `${completionRate}%` }}
                        ></div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })
          ) : (
            <div className="px-6 py-8 text-center">
              <BookOpenIcon className="mx-auto h-12 w-12 text-gray-400 dark:text-gray-300" />
              <h3 className="mt-2 text-sm font-medium text-gray-900 dark:text-white">
                No courses found
              </h3>
              <p className="mt-1 text-sm text-gray-500 dark:text-gray-300">
                Start by creating courses in Google Classroom.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
          Quick Actions
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <button className="flex items-center justify-center px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
            <BookOpenIcon className="h-5 w-5 mr-2" />
            View All Courses
          </button>
          <button className="flex items-center justify-center px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
            <ClipboardDocumentCheckIcon className="h-5 w-5 mr-2" />
            Create Assignment
          </button>
          <button className="flex items-center justify-center px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
            <UsersIcon className="h-5 w-5 mr-2" />
            View Students
          </button>
        </div>
      </div>
    </div>
  );
};

export default TeacherDashboard;
